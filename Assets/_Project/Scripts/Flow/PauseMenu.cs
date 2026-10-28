using Game.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Flow
{
    // Esc ile açılan duraklatma menüsü: devam, odayı sıfırla, çık.
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private InputActionReference pauseAction;
        [SerializeField] private GameObject panel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button resetRoomButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private PlayerRig player;
        [SerializeField] private GameFlow flow;

        private void Awake()
        {
            panel.SetActive(false);
        }

        private void OnEnable()
        {
            pauseAction.action.Enable();
            resumeButton.onClick.AddListener(Resume);
            resetRoomButton.onClick.AddListener(ResetRoom);
            quitButton.onClick.AddListener(Quit);
        }

        private void OnDisable()
        {
            pauseAction.action.Disable();
            resumeButton.onClick.RemoveListener(Resume);
            resetRoomButton.onClick.RemoveListener(ResetRoom);
            quitButton.onClick.RemoveListener(Quit);
        }

        // Time.timeScale 0 iken de Update çalışır; girdi okunabilir.
        private void Update()
        {
            if (pauseAction.action.WasPressedThisFrame())
            {
                if (GamePause.IsPaused)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }
        }

        public void Pause()
        {
            GamePause.SetPaused(true);
            player.SetPaused(true);
            ClearSelection();
            panel.SetActive(true);
        }

        public void Resume()
        {
            ClearSelection();
            panel.SetActive(false);
            player.SetPaused(false);
            GamePause.SetPaused(false);
        }

        // EventSystem son tıklanan butonu "seçili" olarak hatırlar; menü yeniden açılınca
        // o buton seçili/basılı renginde kalır. Her açılış ve kapanışta seçim temizlenir.
        private static void ClearSelection()
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void ResetRoom()
        {
            Resume();
            flow.ResetCurrentRoom();
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
