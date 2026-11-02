using System;
using System.Collections;
using Game.Audio;
using Game.Chamber;
using Game.Narrative;
using Game.Player;
using Game.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Flow
{
    // Oyunun akışı. Core sahnesinde yaşar, hiç kapanmaz.
    // - Sıradaki odayı arka planda yükler ve girişini mevcut odanın çıkışına yerleştirir.
    // - Oyuncu geçiş kabinine girince önceki odayı bellekten atar.
    // - Ölümde ekranı karartır ve sadece oda sahnesini yeniden yükler.
    // - Çıkışı olmayan sahneden (ev gibi hikâye sahnesi) sonrakine karartmayla geçer.
    public class GameFlow : MonoBehaviour
    {
        [SerializeField] private GameSequence sequence;
        [SerializeField] private PlayerRig player;
        [SerializeField] private ScreenFader fader;
        [SerializeField] private RetroAtmosphere atmosphere;
        [SerializeField] private MainMenu mainMenu;
        [SerializeField] private GameSave save;
        [SerializeField] private AmbiencePlayer ambience;

        [Header("Süreler (saniye)")]
        [Tooltip("Ölüm anından karartma başlayana kadar.")]
        [SerializeField] private float deathDuration = 1.5f;
        [Tooltip("Kabinin arka kapısının kapanma süresi. Önceki oda bundan sonra atılır.")]
        [SerializeField] private float airlockCloseDuration = 0.6f;
        [Tooltip("Kabindeki temizlik en az bu kadar sürer. Not okunabilsin, önceki odanın anonsu bitsin.")]
        [SerializeField] private float decontaminationDuration = 5f;
        [Tooltip("Anons sürse bile temizlik en fazla bu kadar sürer.")]
        [SerializeField] private float decontaminationMaxDuration = 14f;

        private RoomRoot current;
        private int currentIndex = -1;
        private Pose currentPose;

        private RoomRoot next;
        private int nextIndex = -1;

        private bool busy;
        private bool preloading;

        private void OnEnable()
        {
            FlowRequests.AdvanceRequested += HandleAdvanceRequested;
        }

        private void OnDisable()
        {
            FlowRequests.AdvanceRequested -= HandleAdvanceRequested;
        }

        // Bir oda sahnesi doğrudan Play'le açıldıysa (test) oyun ondan başlar, kayda dokunulmaz.
        // Yoksa ana menü açılır.
        private IEnumerator Start()
        {
            // Doğrudan açılan oda sahnesinin yüklemesi bitsin.
            yield return null;

            RoomRoot existing = FindLoadedRoom();
            if (existing != null)
            {
                current = existing;
                currentIndex = sequence.IndexOfScene(existing.gameObject.scene.name);
                if (currentIndex < 0)
                {
                    Debug.LogWarning($"{existing.gameObject.scene.name} oyun sırasında yok; sonraki oda yüklenmeyecek.", this);
                }
            }
            else
            {
                yield return mainMenu.Open();
                yield break;
            }

            yield return BeginCurrent();
        }

        public void StartNewGame()
        {
            save.Delete();
            Notebook.Clear();
            save.Recording = true;
            StartCoroutine(LoadAndBegin(0));
        }

        public void ContinueGame()
        {
            if (!save.TryLoad(out string sceneName) || sequence.IndexOfScene(sceneName) < 0)
            {
                StartNewGame();
                return;
            }

            save.Recording = true;
            StartCoroutine(LoadAndBegin(sequence.IndexOfScene(sceneName)));
        }

        // Duraklatma menüsünden: her şey sıfırlanır, Core baştan yüklenir, ana menü açılır.
        public void ReturnToMainMenu()
        {
            GamePause.SetPaused(false);
            ActiveChamber.Set(null);
            Announcer.ClearAll();
            ChamberSession.ClearAll();
            Notebook.Clear();
            SceneManager.LoadScene(gameObject.scene.name, LoadSceneMode.Single);
        }

        private IEnumerator LoadAndBegin(int index)
        {
            if (sequence.Count == 0)
            {
                Debug.LogError("Oyun sırasında hiç sahne yok.", this);
                yield break;
            }

            fader.SetBlack();
            currentIndex = index;
            yield return LoadRoom(sequence[currentIndex].SceneName, room => current = room);
            yield return BeginCurrent();
        }

        private IEnumerator BeginCurrent()
        {
            currentPose = current.RootPose;
            SceneManager.SetActiveScene(current.gameObject.scene);
            player.TeleportTo(current.SpawnPoint);
            if (current.StartsBlack)
            {
                fader.SetBlack();
            }

            OpenAirlockForStart();
            EnterCurrent();

            if (!current.StartsBlack && fader.IsBlack)
            {
                yield return fader.FadeIn();
            }
        }

        private void EnterCurrent()
        {
            atmosphere.SetRoomLighting(current.SunIntensity, current.AmbientColor);
            ambience.Play(current.Ambience, current.AmbienceVolume);

            if (current.Chamber != null)
            {
                current.Chamber.StateChanged += HandleStateChanged;
                ActiveChamber.Set(current.Chamber);
                current.Chamber.Begin();
            }
            else
            {
                ActiveChamber.Set(null);
            }

            current.NotifyEntered();

            if (currentIndex >= 0)
            {
                save.SetScene(sequence[currentIndex].SceneName);
            }

            // Çıkışı olan sahnenin sonrası arka planda yüklenip kapısına hizalanır.
            if (next == null && currentIndex >= 0 && current.ExitAnchor != null)
            {
                StartCoroutine(PreloadNext());
            }
        }

        private void LeaveCurrent()
        {
            if (current.Chamber != null)
            {
                current.Chamber.StateChanged -= HandleStateChanged;
            }

            ActiveChamber.Set(null);
        }

        private void OpenAirlockForStart()
        {
            if (current.Airlock != null)
            {
                current.Airlock.OpenForStart();
            }
        }

        private IEnumerator PreloadNext()
        {
            int index = currentIndex + 1;
            if (index >= sequence.Count)
            {
                yield break;
            }

            preloading = true;
            RoomRoot loaded = null;
            yield return LoadRoom(sequence[index].SceneName, room => loaded = room);
            preloading = false;

            if (loaded == null)
            {
                yield break;
            }

            if (loaded.Airlock == null)
            {
                Debug.LogError($"{sequence[index].SceneName} yürüyerek girilecek ama kabini (Airlock) yok.", loaded);
                yield break;
            }

            loaded.AlignEntryTo(current.ExitAnchor);
            loaded.Airlock.PlayerEntered += HandleAirlockEntered;
            loaded.Airlock.AwaitPlayer();
            next = loaded;
            nextIndex = index;
        }

        private void HandleAirlockEntered(Airlock airlock)
        {
            airlock.PlayerEntered -= HandleAirlockEntered;
            StartCoroutine(TransitionToNext());
        }

        private IEnumerator TransitionToNext()
        {
            while (busy)
            {
                yield return null;
            }

            busy = true;
            float started = Time.time;
            next.Airlock.CloseBack();
            next.Airlock.BeginDecontamination();
            yield return new WaitForSeconds(airlockCloseDuration);

            player.ClearHand();
            LeaveCurrent();
            yield return SceneManager.UnloadSceneAsync(current.gameObject.scene);

            current = next;
            currentIndex = nextIndex;
            currentPose = current.RootPose;
            next = null;

            SceneManager.SetActiveScene(current.gameObject.scene);

            // Odanın saati ve anonsları temizlik bitince başlar.
            yield return new WaitUntil(() =>
            {
                float elapsed = Time.time - started;
                return elapsed >= decontaminationMaxDuration
                       || (elapsed >= decontaminationDuration && !AnnouncementPlayer.IsPlaying);
            });
            current.Airlock.EndDecontamination();

            // Oyuncu notu okuyup kapıyı kendisi açar; oda (saat, anonslar) o anda başlar.
            yield return new WaitUntil(() => current.Airlock.FrontIsOpen);
            EnterCurrent();
            busy = false;
        }

        // Çıkışı olmayan sahne (ev) bittiğinde: karart, sahneyi at, sıradakini yükle.
        private void HandleAdvanceRequested()
        {
            if (busy || next != null || currentIndex < 0 || currentIndex + 1 >= sequence.Count)
            {
                return;
            }

            StartCoroutine(AdvanceWithFade());
        }

        private IEnumerator AdvanceWithFade()
        {
            busy = true;
            player.SetControlEnabled(false);
            if (!fader.IsBlack)
            {
                yield return fader.FadeOut();
            }

            LeaveCurrent();
            player.ClearHand();
            yield return SceneManager.UnloadSceneAsync(current.gameObject.scene);

            currentIndex++;
            yield return LoadRoom(sequence[currentIndex].SceneName, room => current = room);
            currentPose = current.RootPose;
            SceneManager.SetActiveScene(current.gameObject.scene);

            player.TeleportTo(current.SpawnPoint);
            player.SetControlEnabled(true);
            OpenAirlockForStart();
            EnterCurrent();

            if (!current.StartsBlack)
            {
                yield return fader.FadeIn();
            }

            busy = false;
        }

        private void HandleStateChanged(ChamberState previous, ChamberState state)
        {
            if (state == ChamberState.Dead && !busy)
            {
                StartCoroutine(Respawn());
            }
        }

        // Duraklatma menüsünden: odadaki tüm denemeleri ve yankıları silip odayı baştan başlatır.
        public void ResetCurrentRoom()
        {
            if (busy || current == null)
            {
                return;
            }

            StartCoroutine(ResetRoutine());
        }

        private IEnumerator ResetRoutine()
        {
            busy = true;
            player.SetControlEnabled(false);
            yield return fader.FadeOut();
            yield return ReloadCurrentRoom(clearEchoes: true);
        }

        private IEnumerator Respawn()
        {
            busy = true;
            player.SetControlEnabled(false);
            yield return new WaitForSecondsRealtime(deathDuration);
            yield return fader.FadeOut();
            yield return ReloadCurrentRoom(clearEchoes: false);
        }

        // Ekran kararmışken çağrılır. Oda sahnesini atıp yeniden yükler, oyuncuyu başlangıca koyar.
        private IEnumerator ReloadCurrentRoom(bool clearEchoes)
        {
            // Sonraki oda bu odanın çıkışına göre yerleşiyor; yüklemesi bitmeden oda yeniden yüklenmesin.
            while (preloading)
            {
                yield return null;
            }

            string sceneName = current.gameObject.scene.name;
            ChamberDefinition definition = current.Chamber != null ? current.Chamber.Definition : null;
            LeaveCurrent();
            player.ClearHand();
            yield return SceneManager.UnloadSceneAsync(current.gameObject.scene);

            if (clearEchoes)
            {
                if (definition != null)
                {
                    ChamberSession.Clear(definition);
                }

                Announcer.ResetChamber(definition);
            }

            yield return LoadRoom(sceneName, room => current = room);
            current.SetRootPose(currentPose);
            SceneManager.SetActiveScene(current.gameObject.scene);

            player.TeleportTo(current.SpawnPoint);
            player.SetControlEnabled(true);
            OpenAirlockForStart();
            EnterCurrent();

            if (!current.StartsBlack)
            {
                yield return fader.FadeIn();
            }

            busy = false;
        }

        private IEnumerator LoadRoom(string sceneName, Action<RoomRoot> onLoaded)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (operation == null)
            {
                Debug.LogError($"{sceneName} yüklenemedi. Build Settings'te mi?", this);
                onLoaded(null);
                yield break;
            }

            yield return operation;

            RoomRoot room = FindRoomIn(SceneManager.GetSceneByName(sceneName));
            if (room == null)
            {
                Debug.LogError($"{sceneName} sahnesinde RoomRoot yok.", this);
            }

            onLoaded(room);
        }

        private RoomRoot FindLoadedRoom()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene != gameObject.scene)
                {
                    RoomRoot room = FindRoomIn(scene);
                    if (room != null)
                    {
                        return room;
                    }
                }
            }

            return null;
        }

        private static RoomRoot FindRoomIn(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                RoomRoot room = root.GetComponentInChildren<RoomRoot>(true);
                if (room != null)
                {
                    return room;
                }
            }

            return null;
        }
    }
}
