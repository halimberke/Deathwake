using System;
using System.Collections.Generic;
using Game.Chamber;
using UnityEngine;

namespace Game.Narrative
{
    // Odanın anlarına bağlı anonslar: "1. deneme başladı", "ilk ölüm", "2. deneme (ilk yankı)", "çözüldü".
    // Liste Inspector'dan düzenlenir. "Bir kez çal" anonsları ölüp yeniden yüklenince tekrar çalmaz.
    public class ChamberAnnouncements : MonoBehaviour
    {
        public enum Moment
        {
            AttemptStarted,
            Died,
            Solved
        }

        [Serializable]
        private class Entry
        {
            [SerializeField] private Moment moment;
            [Tooltip("Hangi denemede. 0 = her denemede.")]
            [SerializeField] private int attempt = 1;
            [SerializeField] private AnnouncementDefinition announcement;

            public Moment Moment => moment;
            public int Attempt => attempt;
            public AnnouncementDefinition Announcement => announcement;
        }

        [SerializeField] private ChamberController chamber;
        [SerializeField] private List<Entry> entries = new();

        private void OnEnable()
        {
            chamber.StateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            chamber.StateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(ChamberState previous, ChamberState next)
        {
            Moment? moment = next switch
            {
                ChamberState.Running => Moment.AttemptStarted,
                ChamberState.Dead => Moment.Died,
                ChamberState.Solved => Moment.Solved,
                _ => null
            };

            if (moment == null)
            {
                return;
            }

            // Deneme sayısı, deneme başlarken artar: 1. denemede 1.
            int attempt = chamber.Progress.AttemptCount;
            foreach (Entry entry in entries)
            {
                if (entry.Moment == moment && (entry.Attempt == 0 || entry.Attempt == attempt))
                {
                    Announcer.Play(entry.Announcement);
                }
            }
        }
    }
}
