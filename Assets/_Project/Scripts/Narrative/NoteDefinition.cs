using UnityEngine;

namespace Game.Narrative
{
    public enum NoteStyle
    {
        Typed,       // Personel formu, rapor: daktilo
        Handwritten  // Denek notu: el yazısı
    }

    // Okunabilir bir not. Dünyadaki NoteItem bunu gösterir, okununca deftere girer.
    [CreateAssetMenu(fileName = "Note_", menuName = "Game/Note")]
    public class NoteDefinition : ScriptableObject
    {
        [SerializeField] private string title = "Not";
        [TextArea(6, 16)]
        [SerializeField] private string body = "...";
        [SerializeField] private NoteStyle style = NoteStyle.Typed;

        public string Title => title;
        public string Body => body;
        public NoteStyle Style => style;
    }
}
