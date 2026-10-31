using System;
using System.Collections;
using Game.Audio;
using Game.Player;
using Game.Settings;
using Game.UI;
using Game.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Flow
{
    // Ana menü: Devam Et (kayıt varsa), Yeni Oyun, Ayarlar, Çıkış. Arkada canlı 3D sahne döner.
    // GameFlow oyun doğrudan bir odadan başlamadıysa bunu açar. Menüdeyken zaman akar (sahne canlı),
    // oyuncu ise yerinde durur.
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private UIPanel panel;
        [SerializeField] private MenuItem continueItem;
        [SerializeField] private MenuItem newGameItem;
        [SerializeField] private MenuItem settingsItem;
        [SerializeField] private MenuItem quitItem;
        [SerializeField] private GameFlow flow;
        [SerializeField] private GameSave save;
        [SerializeField] private PlayerRig player;
        [SerializeField] private SettingsPanel settings;
        [SerializeField] private ConfirmDialog confirm;
        [SerializeField] private ScreenFader fader;

        [Header("Arka plan")]
        [SerializeField] private string backdropScene = "MenuBackdrop";
        [SerializeField] private RetroScreen retroScreen;
        [SerializeField] private RetroAtmosphere atmosphere;
        [SerializeField] private float backdropSun = 0.25f;
        [SerializeField] private Color backdropAmbient = new Color(0.2f, 0.22f, 0.26f);
        [SerializeField] private ProceduralSound ambience;
        [Tooltip("Menü açıkken gizlenen oyun içi arayüz (nişangâh, yazılar).")]
        [SerializeField] private GameObject[] hideWhileOpen;

        [Header("Geçiş (sn)")]
        [SerializeField] private float fadeInDuration = 1.2f;
        [SerializeField] private float fadeOutDuration = 0.7f;

        [Header("Yazılar")]
        [SerializeField] private string overwriteTitle = "Yeni oyun başlatılsın mı?";
        [SerializeField] private string overwriteBody = "Mevcut kayıt silinecek. Bu işlem geri alınamaz.";
        [SerializeField] private string overwriteConfirm = "Kaydı sil ve başla";

        public static bool IsOpen { get; private set; }

        private bool leaving;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            IsOpen = false;
        }

        private void OnEnable()
        {
            continueItem.Button.onClick.AddListener(Continue);
            newGameItem.Button.onClick.AddListener(NewGame);
            settingsItem.Button.onClick.AddListener(OpenSettings);
            quitItem.Button.onClick.AddListener(Quit);
            settings.Closed += HandleSettingsClosed;
        }

        private void OnDisable()
        {
            continueItem.Button.onClick.RemoveListener(Continue);
            newGameItem.Button.onClick.RemoveListener(NewGame);
            settingsItem.Button.onClick.RemoveListener(OpenSettings);
            quitItem.Button.onClick.RemoveListener(Quit);
            settings.Closed -= HandleSettingsClosed;
            IsOpen = false;
        }

        public IEnumerator Open()
        {
            IsOpen = true;
            leaving = false;
            fader.SetBlack();
            player.SetMenuMode(true);
            SetGameHudVisible(false);

            yield return SceneManager.LoadSceneAsync(backdropScene, LoadSceneMode.Additive);
            if (MenuBackdrop.Current != null)
            {
                retroScreen.UseCamera(MenuBackdrop.Current.View);
            }

            atmosphere.SetRoomLighting(backdropSun, backdropAmbient);
            ambience.Play();

            bool hasSave = save.HasSave;
            continueItem.gameObject.SetActive(hasSave);
            panel.Show(hasSave ? continueItem.Button : newGameItem.Button);
            yield return fader.FadeTo(0f, fadeInDuration);
        }

        // Karart, arka planı at, oyunu başlat.
        private IEnumerator Leave(Action startGame)
        {
            leaving = true;
            panel.SetInteractable(false);
            yield return fader.FadeTo(1f, fadeOutDuration);

            ambience.Stop();
            panel.HideImmediate();
            retroScreen.ClearCamera();
            yield return SceneManager.UnloadSceneAsync(backdropScene);

            SetGameHudVisible(true);
            player.SetMenuMode(false);
            IsOpen = false;
            startGame();
        }

        private void Continue()
        {
            if (!leaving)
            {
                StartCoroutine(Leave(flow.ContinueGame));
            }
        }

        private void NewGame()
        {
            if (leaving)
            {
                return;
            }

            if (save.HasSave)
            {
                confirm.Ask(overwriteTitle, overwriteBody, overwriteConfirm, panel,
                    () => StartCoroutine(Leave(flow.StartNewGame)));
                return;
            }

            StartCoroutine(Leave(flow.StartNewGame));
        }

        private void OpenSettings()
        {
            panel.Hide();
            settings.Open();
        }

        private void HandleSettingsClosed()
        {
            if (IsOpen)
            {
                panel.Show(settingsItem.Button);
            }
        }

        private void SetGameHudVisible(bool visible)
        {
            foreach (GameObject item in hideWhileOpen)
            {
                item.SetActive(visible);
            }
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
