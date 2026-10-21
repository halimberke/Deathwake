using Game.Chamber;
using UnityEngine;

namespace Game.Flow
{
    // Bir oda sahnesinin kökü. Sahnedeki her şey bunun altında durur ki oda bütün olarak taşınabilsin.
    // Oda, girişi önceki odanın çıkışına denk gelecek şekilde yerleştirilir.
    public class RoomRoot : MonoBehaviour
    {
        [SerializeField] private ChamberController chamber;
        [SerializeField] private Airlock airlock;
        [Tooltip("Odanın girişi. İleri yönü (mavi ok) yürüme yönünü gösterir.")]
        [SerializeField] private Transform entryAnchor;
        [Tooltip("Odanın çıkışı. Sonraki odanın girişi buraya yerleşir.")]
        [SerializeField] private Transform exitAnchor;
        [Tooltip("Ölümden sonra oyuncunun başladığı nokta.")]
        [SerializeField] private Transform spawnPoint;

        public ChamberController Chamber => chamber;
        public Airlock Airlock => airlock;
        public Transform ExitAnchor => exitAnchor;
        public Transform SpawnPoint => spawnPoint;

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
