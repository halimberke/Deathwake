using Game.Chamber;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Flow
{
    // Bir oda sahnesinin kökü. Sahnedeki her şey bunun altında durur ki oda bütün olarak taşınabilsin.
    // Oda, girişi önceki odanın çıkışına denk gelecek şekilde yerleştirilir.
    public class RoomRoot : MonoBehaviour
    {
        [Tooltip("Boşsa hikâye sahnesidir: saat, ölüm ve yankı yok.")]
        [SerializeField] private ChamberController chamber;
        [Tooltip("Boşsa sahneye önceki sahnenin çıkışından yürüyerek değil, karartmayla gelinir.")]
        [SerializeField] private Airlock airlock;
        [Tooltip("Odanın girişi. İleri yönü (mavi ok) yürüme yönünü gösterir.")]
        [SerializeField] private Transform entryAnchor;
        [Tooltip("Odanın çıkışı. Sonraki odanın girişi buraya yerleşir. Boşsa sonraki sahneye karartmayla geçilir.")]
        [SerializeField] private Transform exitAnchor;
        [Tooltip("Ölümden sonra oyuncunun başladığı nokta.")]
        [SerializeField] private Transform spawnPoint;
        [Tooltip("Açıksa sahneye girildiğinde ekran karanlık kalır; açılışı sahnenin kendi senaryosu yapar.")]
        [SerializeField] private bool startsBlack;
        [Tooltip("Oyuncu bu sahneye girdiğinde (oda başladığında). Senaryolar buradan başlatılır.")]
        [SerializeField] private UnityEvent onEntered;

        public ChamberController Chamber => chamber;
        public Airlock Airlock => airlock;
        public Transform ExitAnchor => exitAnchor;
        public Transform SpawnPoint => spawnPoint;
        public bool StartsBlack => startsBlack;

        public void NotifyEntered()
        {
            onEntered.Invoke();
        }

        public Pose RootPose => new Pose(transform.position, transform.rotation);

        // Odayı, girişi hedefle aynı konum ve yöne gelecek şekilde döndürüp taşır.
        public void AlignEntryTo(Transform target)
        {
            Quaternion delta = target.rotation * Quaternion.Inverse(entryAnchor.rotation);
            transform.rotation = delta * transform.rotation;
            transform.position += target.position - entryAnchor.position;
            // Fizik dünyası taşınan collider'ları hemen görsün.
            Physics.SyncTransforms();
        }

        public void SetRootPose(Pose pose)
        {
            transform.SetPositionAndRotation(pose.position, pose.rotation);
            Physics.SyncTransforms();
        }
    }
}
