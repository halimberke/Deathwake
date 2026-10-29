using System;
using UnityEngine;

namespace Game.Flow
{
    // Sahnelerin oyun akışından istekleri. Hikâye sahnesi Core'daki GameFlow'u doğrudan tanımaz;
    // "sonraki sahneye geç" isteğini buraya bırakır.
    public static class FlowRequests
    {
        public static event Action AdvanceRequested;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            AdvanceRequested = null;
        }

        public static void Advance()
        {
            AdvanceRequested?.Invoke();
        }
    }
}
