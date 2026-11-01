using Game.Localization;
using Game.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Settings
{
    // Core'da yaşar. Ayarları açılışta yükler ve her değişimde oyuna uygular.
    public class SettingsApplier : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private PlayerLook look;

        private void Awake()
        {
            GameSettings.Load();
            Loc.SetLanguage(GameSettings.Language);
            if (!string.IsNullOrEmpty(GameSettings.BindingOverrides))
            {
                actions.LoadBindingOverridesFromJson(GameSettings.BindingOverrides);
                KeyLabels.Invalidate();
            }
        }

        private void OnEnable()
        {
            GameSettings.Changed += Apply;
        }

        private void OnDisable()
        {
            GameSettings.Changed -= Apply;
        }

        private void Start()
        {
            Apply();
        }

        private void Apply()
        {
            look.SetSensitivity(GameSettings.Sensitivity);
            AudioListener.volume = GameSettings.Volume;
            Loc.SetLanguage(GameSettings.Language);

            // Editor'ün Game penceresi bu ayarları kullanmaz.
            if (Application.isEditor)
            {
                return;
            }

            FullScreenMode mode = GameSettings.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            int width = GameSettings.Width > 0 ? GameSettings.Width : Display.main.systemWidth;
            int height = GameSettings.Height > 0 ? GameSettings.Height : Display.main.systemHeight;
            if (Screen.fullScreenMode != mode || Screen.width != width || Screen.height != height)
            {
                Screen.SetResolution(width, height, mode);
            }
        }
    }
}
