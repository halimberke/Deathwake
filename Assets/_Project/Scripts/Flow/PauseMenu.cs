using Game.Chamber;
using Game.Narrative;
using Game.Player;
using Game.Settings;
using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Flow
{
    // Esc ile açılan duraklatma menüsü. Esc'in bütün önceliği burada toplanır:
    // onay penceresi, ayarlar, not/defter, sonra duraklatma.
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private InputActionReference pauseAction;
        [SerializeField] private UIPanel panel;
        [SerializeField] private TMP_Text roomLabel;
        [SerializeField] private MenuItem resumeItem;
        [SerializeField] private MenuItem settingsItem;
        [SerializeField] private MenuItem resetRoomItem;
        [SerializeField] private MenuItem mainMenuItem;
        [SerializeField] private MenuItem quitItem;
        [SerializeField] private SettingsPanel settings;
        [SerializeField] private ConfirmDialog confirm;
        [SerializeField] private PlayerRig player;
        [SerializeField] private GameFlow flow;
        [Tooltip("Menü açıkken gizlenen oyun içi arayüz.")]
        [SerializeField] private GameObject[] hideWhileOpen;

        [Header("Yazılar")]
        [Tooltip("{0} = deney kodu, {1} = oda adı, {2} = deneme sayısı.")]
        [SerializeField] private string roomFormat = "{0}  /  {1}  /  DENEME {2}";
        [SerializeField] private string leaveBody = "Bu odadaki ilerleme kaybolacak. Oyun son kayıttan devam eder.";
        [SerializeField] private string resetTitle = "Oda sıfırlansın mı?";
        [SerializeField] private string resetBody = "Bu odadaki bütün denemeler ve yankılar silinecek.";
        [SerializeField] private string resetConfirm = "Odayı sıfırla";
        [SerializeField] private string menuTitle = "Ana menüye dönülsün mü?";
        [SerializeField] private string menuConfirm = "Ana menüye dön";
        [SerializeField] private string quitTitle = "Oyundan çıkılsın mı?";
        [SerializeField] private string quitConfirm = "Çık";

        private void OnEnable()
        {
            pauseAction.action.Enable();
            resumeItem.Button.onClick.AddListener(Resume);
            settingsItem.Button.onClick.AddListener(OpenSettings);
            resetRoomItem.Button.onClick.AddListener(AskResetRoom);
            mainMenuItem.Button.onClick.AddListener(AskMainMenu);
            quitItem.Button.onClick.AddListener(AskQuit);
            settings.Closed += HandleSettingsClosed;
        }

        private void OnDisable()
        {
            pauseAction.action.Disable();
            resumeItem.Button.onClick.RemoveListener(Resume);
            settingsItem.Button.onClick.RemoveListener(OpenSettings);
            resetRoomItem.Button.onClick.RemoveListener(AskResetRoom);
            mainMenuItem.Button.onClick.RemoveListener(AskMainMenu);
            quitItem.Button.onClick.RemoveListener(AskQuit);
            settings.Closed -= HandleSettingsClosed;
        }

        // Time.timeScale 0 iken de Update çalışır; girdi okunabilir.
        private void Update()
        {
            if (!pauseAction.action.WasPressedThisFrame())
            {
                return;
            }

            if (confirm.IsOpen)
            {
                confirm.Cancel();
                return;
            }

            // Ayarlar ana menüden de duraklatmadan da açılmış olabilir.
            if (settings.IsOpen)
            {
                if (!settings.BlocksClose)
                {
                    settings.Close();
                }

                return;
            }

            // Not ya da defter açıksa Esc onu kapatır, menüyü açmaz. Ana menüde Esc işlemez.
            if (NotebookUI.BlocksPause || MainMenu.IsOpen)
            {
                return;
            }

            if (GamePause.IsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        public void Pause()
        {
            GamePause.SetPaused(true);
            player.SetPaused(true);
            SetGameHudVisible(false);
            RefreshRoomLabel();

            // Oda dışında (ev, hücre) sıfırlanacak bir deneme yok.
            resetRoomItem.gameObject.SetActive(ActiveChamber.Current != null);
            panel.Show(resumeItem.Button);
        }

        public void Resume()
        {
            panel.Hide();
            SetGameHudVisible(true);
            player.SetPaused(false);
            GamePause.SetPaused(false);
        }

        private void RefreshRoomLabel()
        {
            ChamberController chamber = ActiveChamber.Current;
            roomLabel.text = chamber == null
                ? string.Empty
                : string.Format(roomFormat, chamber.Definition.TitleCode,
                    chamber.Definition.DisplayName.ToUpperInvariant(), chamber.Progress.AttemptCount);
        }

        private void SetGameHudVisible(bool visible)
        {
            foreach (GameObject item in hideWhileOpen)
            {
                item.SetActive(visible);
            }
        }

        private void OpenSettings()
        {
            panel.Hide();
            settings.Open();
        }

        private void HandleSettingsClosed()
        {
            if (GamePause.IsPaused && !MainMenu.IsOpen)
            {
                panel.Show(settingsItem.Button);
            }
        }

        private void AskResetRoom()
        {
            confirm.Ask(resetTitle, resetBody, resetConfirm, panel, () =>
            {
                Resume();
                flow.ResetCurrentRoom();
            });
        }

        private void AskMainMenu()
        {
            confirm.Ask(menuTitle, leaveBody, menuConfirm, panel, flow.ReturnToMainMenu);
        }

        private void AskQuit()
        {
            confirm.Ask(quitTitle, leaveBody, quitConfirm, panel, Quit);
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
