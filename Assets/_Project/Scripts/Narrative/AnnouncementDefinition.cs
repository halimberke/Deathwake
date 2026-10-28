using UnityEngine;

namespace Game.Narrative
{
    // Bir anons: kim konuşuyor ve hangi satırları söylüyor. Her satır ayrı altyazı olarak görünür.
    [CreateAssetMenu(fileName = "Announcement_", menuName = "Game/Announcement")]
    public class AnnouncementDefinition : ScriptableObject
    {
        [SerializeField] private SpeakerVoice speaker;
        [TextArea(2, 4)]
        [SerializeField] private string[] lines = { "..." };
        [Tooltip("Açıksa oyun boyunca bir kez çalar. Oda tekrar denenince yeniden duyulmaz.")]
        [SerializeField] private bool playOnce = true;
        [Tooltip("Satırlar arasındaki sessizlik (sn).")]
        [SerializeField] private float linePause = 0.4f;

        public SpeakerVoice Speaker => speaker;
        public string[] Lines => lines;
        public bool PlayOnce => playOnce;
        public float LinePause => linePause;
    }
}
