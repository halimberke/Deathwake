using UnityEngine;

namespace Game.Narrative
{
    // Odadaki anons tetikleyicisi. İki yolla çalışır:
    // - Play() bir UnityEvent'ten çağrılır (örnek: ChamberTimeline'da "3. sn'de anons").
    // - Collider'ı trigger ise oyuncu alana girince çalar (örnek: koridor).
    public class AnnouncementTrigger : MonoBehaviour
    {
        [SerializeField] private AnnouncementDefinition announcement;
        [Tooltip("Açıksa trigger collider'a oyuncu girince çalar.")]
        [SerializeField] private bool playOnPlayerEnter;

        public void Play()
        {
            Announcer.Play(announcement);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (playOnPlayerEnter && other.CompareTag("Player"))
            {
                Play();
            }
        }
    }
}
