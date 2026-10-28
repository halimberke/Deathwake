using System;
using System.Collections.Generic;
using Game.Chamber;
using UnityEngine;

namespace Game.Narrative
{
    // Anons isteklerinin tek giriş kapısı. Oda sahnesindeki nesneler Core'daki oynatıcıyı doğrudan
    // tanımaz (ayrı sahneler); isteği buraya bırakır, oynatıcı dinler.
    public static class Announcer
    {
        public static event Action<AnnouncementDefinition> Requested;
        public static event Action Stopped;

        // "Bir kez çal" anonsları sahne dışında hatırlanır; ölüp oda yeniden yüklenince tekrar çalmaz.
        // Hangi odada çalındığı da tutulur: oda sıfırlanınca o odanın anonsları unutulur.
        private static readonly Dictionary<AnnouncementDefinition, ChamberDefinition> played = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Requested = null;
            Stopped = null;
            played.Clear();
        }

        public static void Play(AnnouncementDefinition announcement)
        {
            if (announcement == null)
            {
                return;
            }

            if (announcement.PlayOnce)
            {
                if (played.ContainsKey(announcement))
                {
                    return;
                }

                ChamberController chamber = ActiveChamber.Current;
                played[announcement] = chamber != null ? chamber.Definition : null;
            }

            if (Requested == null)
            {
                Debug.LogWarning($"Anons çalınamadı, oynatıcı yok: {announcement.name}");
                return;
            }

            Requested.Invoke(announcement);
        }

        // "Odayı sıfırla": çalan anons kesilir, odanın anonsları baştan çalabilir.
        public static void ResetChamber(ChamberDefinition chamber)
        {
            var forgotten = new List<AnnouncementDefinition>();
            foreach (KeyValuePair<AnnouncementDefinition, ChamberDefinition> pair in played)
            {
                if (pair.Value == chamber)
                {
                    forgotten.Add(pair.Key);
                }
            }

            foreach (AnnouncementDefinition announcement in forgotten)
            {
                played.Remove(announcement);
            }

            Stopped?.Invoke();
        }
    }
}
