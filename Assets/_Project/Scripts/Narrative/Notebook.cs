using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Narrative
{
    // Oyuncunun defteri: okunan notlar. Sahne dışında yaşar; ölüm ve oda sıfırlama silmez.
    // "Bilgi kalıcı, eşya değil."
    public static class Notebook
    {
        private static readonly List<NoteDefinition> notes = new();

        // Not okunmak istendi. İkinci değer: deftere yeni mi girdi.
        public static event Action<NoteDefinition, bool> ReadRequested;

        public static IReadOnlyList<NoteDefinition> Notes => notes;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            notes.Clear();
            ReadRequested = null;
        }

        public static void Read(NoteDefinition note)
        {
            if (note == null)
            {
                return;
            }

            bool isNew = !notes.Contains(note);
            if (isNew)
            {
                notes.Add(note);
            }

            ReadRequested?.Invoke(note, isNew);
        }
    }
}
