using System;
using UnityEngine;

namespace Game.Chamber
{
    // "Şu an oynanan oda hangisi?" sorusunun tek cevabı.
    // Oyuncu, HUD ve kayıt sistemi odaya doğrudan bağlanmaz; bunu dinler.
    // Odalar ayrı sahnelerde yüklenip atıldığı için doğrudan referans tutulamaz.
    public static class ActiveChamber
    {
        public static ChamberController Current { get; private set; }

        public static event Action<ChamberController> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Current = null;
            Changed = null;
        }

        public static void Set(ChamberController chamber)
        {
            if (Current == chamber)
            {
                return;
            }

            Current = chamber;
            Changed?.Invoke(chamber);
        }
    }
}
