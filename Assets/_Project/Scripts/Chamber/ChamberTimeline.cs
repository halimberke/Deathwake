using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Chamber
{
    // Odanın zaman çizelgesi: "X. saniyede şu olayı tetikle" listesi.
    // Örnek: 10. sn'de kepengi kapat, 25. sn'de anahtarı bırak. Süreler inspector'dan girilir.
    public class ChamberTimeline : MonoBehaviour
    {
        [Serializable]
        private class Entry
        {
            [SerializeField] private string label;
            [SerializeField] private float time;
            [SerializeField] private UnityEvent action;

            public string Label => label;
            public float Time => time;
            public UnityEvent Action => action;
        }

        [SerializeField] private ChamberController chamber;
        [SerializeField] private List<Entry> entries = new();

        private readonly List<Entry> pending = new();

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
            if (next != ChamberState.Running)
            {
                pending.Clear();
                return;
            }

            // Deneme başında listeyi zamana göre sıralı bir kopyaya al.
            pending.Clear();
            pending.AddRange(entries);
            pending.Sort((a, b) => a.Time.CompareTo(b.Time));
        }

        // Saat her kare ilerler; sırası gelen olaylar tetiklenir. Süreler veride, kodda sabit zaman yok.
        private void Update()
        {
            float now = chamber.Clock.Time;

            while (pending.Count > 0 && pending[0].Time <= now)
            {
                Entry entry = pending[0];
                pending.RemoveAt(0);
                entry.Action.Invoke();
            }
        }
    }
}
