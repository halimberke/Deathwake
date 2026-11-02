using System;
using UnityEngine;

namespace Game.Settings
{
    // Oyuncu ayarları. Sahne dışında yaşar, PlayerPrefs ile diske yazılır.
    // Ayarı değiştiren arayüz, uygulayan SettingsApplier: ikisi birbirini tanımaz, Changed olayı bağlar.
    public static class GameSettings
    {
        public const float DefaultSensitivity = 0.1f;
        public const float MinSensitivity = 0.02f;
        public const float MaxSensitivity = 0.4f;

        private const string SensitivityKey = "settings.sensitivity";
        private const string VolumeKey = "settings.volume";
        private const string FullscreenKey = "settings.fullscreen";
        private const string WidthKey = "settings.width";
        private const string HeightKey = "settings.height";
        private const string BindingsKey = "settings.bindings";
        private const string LanguageKey = "settings.language";
        private const string DefaultLanguage = "en";

        public static event Action Changed;

        public static float Sensitivity { get; private set; } = DefaultSensitivity;
        public static float Volume { get; private set; } = 1f;
        public static bool Fullscreen { get; private set; } = true;
        public static int Width { get; private set; }  // 0 = masaüstü çözünürlüğü
        public static int Height { get; private set; }
        public static string BindingOverrides { get; private set; } = string.Empty;
        public static string Language { get; private set; }

        public static void Load()
        {
            Sensitivity = PlayerPrefs.GetFloat(SensitivityKey, DefaultSensitivity);
            Volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
            Fullscreen = PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
            Width = PlayerPrefs.GetInt(WidthKey, 0);
            Height = PlayerPrefs.GetInt(HeightKey, 0);
            BindingOverrides = PlayerPrefs.GetString(BindingsKey, string.Empty);
            Language = PlayerPrefs.GetString(LanguageKey, DefaultLanguage);
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat(SensitivityKey, Sensitivity);
            PlayerPrefs.SetFloat(VolumeKey, Volume);
            PlayerPrefs.SetInt(FullscreenKey, Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(WidthKey, Width);
            PlayerPrefs.SetInt(HeightKey, Height);
            PlayerPrefs.SetString(BindingsKey, BindingOverrides);
            PlayerPrefs.SetString(LanguageKey, Language);
            PlayerPrefs.Save();
        }

        public static void SetSensitivity(float value)
        {
            Sensitivity = Mathf.Clamp(value, MinSensitivity, MaxSensitivity);
            Changed?.Invoke();
        }

        public static void SetVolume(float value)
        {
            Volume = Mathf.Clamp01(value);
            Changed?.Invoke();
        }

        public static void SetFullscreen(bool value)
        {
            Fullscreen = value;
            Changed?.Invoke();
        }

        public static void SetResolution(int width, int height)
        {
            Width = width;
            Height = height;
            Changed?.Invoke();
        }

        public static void SetLanguage(string code)
        {
            Language = code;
            Changed?.Invoke();
        }

        // Tuş atamaları Input System'in kendi JSON biçiminde saklanır. Uygulayan SettingsPanel/Applier.
        public static void SetBindingOverrides(string json)
        {
            BindingOverrides = json ?? string.Empty;
        }
    }
}
