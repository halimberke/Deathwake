using System.Collections.Generic;
using UnityEngine;

namespace Game.Narrative
{
    // Oyundaki bütün notların listesi. Kayıt dosyası notları adıyla tutar; yüklerken buradan bulunur.
    // Yeni not eklenince buraya da eklenmeli, yoksa kayıttan geri gelmez.
    [CreateAssetMenu(fileName = "NoteCatalog", menuName = "Game/Note Catalog")]
    public class NoteCatalog : ScriptableObject
    {
        [SerializeField] private List<NoteDefinition> notes = new();

        public NoteDefinition Find(string id)
        {
            return notes.Find(note => note != null && note.name == id);
        }
    }
}
