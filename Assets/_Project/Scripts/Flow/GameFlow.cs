using System;
using System.Collections;
using Game.Chamber;
using Game.Narrative;
using Game.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Flow
{
    // Oyunun akışı. Core sahnesinde yaşar, hiç kapanmaz.
    // - Sıradaki odayı arka planda yükler ve girişini mevcut odanın çıkışına yerleştirir.
    // - Oyuncu geçiş kabinine girince önceki odayı bellekten atar.
    // - Ölümde ekranı karartır ve sadece oda sahnesini yeniden yükler.
    public class GameFlow : MonoBehaviour
    {
        [SerializeField] private GameSequence sequence;
        [SerializeField] private PlayerRig player;
        [SerializeField] private ScreenFader fader;

        [Header("Süreler (saniye)")]
        [Tooltip("Ölüm anından karartma başlayana kadar.")]
        [SerializeField] private float deathDuration = 1.5f;
        [Tooltip("Kabinin arka kapısının kapanma süresi. Önceki oda bundan sonra atılır.")]
        [SerializeField] private float airlockCloseDuration = 0.6f;

        private RoomRoot current;
        private int currentIndex = -1;
        private Pose currentPose;

        private RoomRoot next;
        private int nextIndex = -1;

        private bool busy;
        private bool preloading;

        // Bir oda sahnesi doğrudan Play'le açıldıysa oyun ondan başlar; yoksa sıranın ilk odasından.
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
                currentIndex = FirstRoomIndex(0);
                if (currentIndex < 0)
                {
                    Debug.LogError("Oyun sırasında hiç oda yok.", this);
                    yield break;
                }

                yield return LoadRoom(sequence[currentIndex].SceneName, room => current = room);
            }

            currentPose = current.RootPose;
            SceneManager.SetActiveScene(current.gameObject.scene);
            player.TeleportTo(current.SpawnPoint);
            current.Airlock.OpenForStart();
            EnterCurrent();
        }

        private void EnterCurrent()
        {
            current.Chamber.StateChanged += HandleStateChanged;
            ActiveChamber.Set(current.Chamber);
            current.Chamber.Begin();

            if (next == null && currentIndex >= 0)
            {
                StartCoroutine(PreloadNext());
            }
        }

        private void LeaveCurrent()
        {
            current.Chamber.StateChanged -= HandleStateChanged;
            ActiveChamber.Set(null);
        }

        private IEnumerator PreloadNext()
        {
            int index = FirstRoomIndex(currentIndex + 1);
            if (index < 0)
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
            next.Airlock.CloseBack();
            yield return new WaitForSeconds(airlockCloseDuration);

            player.ClearHand();
            LeaveCurrent();
            yield return SceneManager.UnloadSceneAsync(current.gameObject.scene);

            current = next;
            currentIndex = nextIndex;
            currentPose = current.RootPose;
            next = null;

            SceneManager.SetActiveScene(current.gameObject.scene);
            current.Airlock.OpenFront();
            EnterCurrent();
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
            ChamberDefinition definition = current.Chamber.Definition;
            LeaveCurrent();
            player.ClearHand();
            yield return SceneManager.UnloadSceneAsync(current.gameObject.scene);

            if (clearEchoes)
            {
                ChamberSession.Clear(definition);
                Announcer.ResetChamber(definition);
            }

            yield return LoadRoom(sceneName, room => current = room);
            current.SetRootPose(currentPose);
            SceneManager.SetActiveScene(current.gameObject.scene);

            player.TeleportTo(current.SpawnPoint);
            player.SetControlEnabled(true);
            current.Airlock.OpenForStart();
            EnterCurrent();

            yield return fader.FadeIn();
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

        private int FirstRoomIndex(int from)
        {
            for (int i = from; i < sequence.Count; i++)
            {
                if (sequence[i].Type == GameSequence.StepType.Room)
                {
                    return i;
                }

                Debug.Log($"Ara sahne adımı henüz desteklenmiyor, atlandı: {sequence[i].SceneName}", this);
            }

            return -1;
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
