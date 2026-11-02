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

        [Header("Ses")]
        [Tooltip("Oda boyunca çalan ortam sesi. Boşsa ortam susar.")]
        [SerializeField] private AudioClip ambience;
        [Range(0f, 1f)]
        [SerializeField] private float ambienceVolume = 0.25f;

        [Header("Işık")]
        [Tooltip("Core'daki genel (yön) ışığın gücü. Gece ya da kapalı mekân için 0.")]
        [SerializeField] private float sunIntensity = 1.2f;
        [Tooltip("Hiçbir lambanın aydınlatmadığı yerlerin rengi.")]
        [SerializeField] private Color ambientColor = new Color(0.35f, 0.36f, 0.4f);

        public ChamberController Chamber => chamber;
        public Airlock Airlock => airlock;
        public Transform ExitAnchor => exitAnchor;
        public Transform SpawnPoint => spawnPoint;
        public bool StartsBlack => startsBlack;
        public float SunIntensity => sunIntensity;
        public Color AmbientColor => ambientColor;
        public AudioClip Ambience => ambience;
        public float AmbienceVolume => ambienceVolume;

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
