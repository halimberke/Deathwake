using System;
using UnityEngine;

namespace Game.UI
{
    // Arayüz seslerinin tek girişi. Öğeler çalanı tanımaz; Core'daki UIAudioPlayer dinler.
    public static class UIAudio
    {
        public static event Action Hovered;
        public static event Action Clicked;

        private static int silentFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Hovered = null;
            Clicked = null;
            silentFrame = -1;
        }

        // Kodla yapılan seçim (panel açılınca ilk öğe) ses çıkarmasın.
        public static void SilenceThisFrame()
        {
            silentFrame = Time.frameCount;
        }

        public static void Hover()
        {
            if (Time.frameCount != silentFrame)
            {
                Hovered?.Invoke();
            }
        }

        public static void Click()
        {
            Clicked?.Invoke();
        }
    }
}
