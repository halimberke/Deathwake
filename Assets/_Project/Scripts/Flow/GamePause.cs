using System;
using UnityEngine;

namespace Game.Flow
{
    // Oyunun duraklatılma durumu. Zamanı durdurur; Time.timeScale'e dokunan diğer sistemler
    // (zaman hızlandırma) duraklatma sürerken ona dokunmaz.
    public static class GamePause
    {
        public static bool IsPaused { get; private set; }

        public static event Action<bool> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            IsPaused = false;
            Changed = null;
        }

        public static void SetPaused(bool paused)
        {
            if (IsPaused == paused)
            {
                return;
            }

            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            Changed?.Invoke(paused);
        }
    }
}
