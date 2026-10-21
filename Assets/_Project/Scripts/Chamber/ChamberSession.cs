using System.Collections.Generic;
using UnityEngine;

namespace Game.Chamber
{
    // Sahne dışında yaşayan oda ilerlemesi. Static olduğu için sahne yeniden yüklenince silinmez.
    public static class ChamberSession
    {
        private static readonly Dictionary<ChamberDefinition, ChamberProgress> progressByChamber = new();

        // Editor'de "domain reload" kapalıysa static alanlar Play'ler arasında kalır.
        // Bu metot her Play başında çağrılır ve eski oturumu temizler.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            progressByChamber.Clear();
        }

        public static ChamberProgress Get(ChamberDefinition chamber)
        {
            if (!progressByChamber.TryGetValue(chamber, out ChamberProgress progress))
            {
                progress = new ChamberProgress();
                progressByChamber[chamber] = progress;
            }

            return progress;
        }

        // "Odayı sıfırla": tüm denemeler ve yankılar silinir.
        public static void Clear(ChamberDefinition chamber)
        {
            progressByChamber.Remove(chamber);
        }
    }
}
