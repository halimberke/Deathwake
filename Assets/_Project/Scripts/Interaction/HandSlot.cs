using System;
using UnityEngine;

namespace Game.Interaction
{
    // Oyuncunun (ve yankının) tek eşyalık eli. Envanter yok.
    // Bırakma "önizlemeli yerleştirme"dir: eşya nişan alınan yüzeye, önizlemede görülen pozla konur.
    public class HandSlot : MonoBehaviour
    {
        [Tooltip("Eşyanın elde durduğu nokta. Kameranın alt nesnesi olmalı.")]
        [SerializeField] private Transform holdPoint;
        [Tooltip("Nişan alınan yön (göz). Genelde ana kamera.")]
        [SerializeField] private Transform dropOrigin;

        [Header("Yerleştirme")]
        [Tooltip("En uzak yerleştirme mesafesi.")]
        [SerializeField] private float placeRange = 3f;
        [Tooltip("Yön bu açının katlarına yuvarlanır (düzgün dizmek için). 0 = serbest.")]
        [SerializeField] private float yawSnap = 15f;
        [Tooltip("Bu kadar yukarı bakan yüzey 'üstüne konulabilir' sayılır (normalin y bileşeni).")]
        [SerializeField] private float floorNormalThreshold = 0.7f;
        [Tooltip("Eşya ile yüzey arasındaki küçük boşluk.")]
        [SerializeField] private float gap = 0.02f;
        [Tooltip("Taşıyanın gövdesine en az bu kadar uzak olmalı (üstüne ya da içine konmasın).")]
        [SerializeField] private float bodyMargin = 0.15f;
        [Tooltip("Zemin aranırken bakılan en büyük derinlik.")]
        [SerializeField] private float groundSearchDepth = 4f;

        [Header("Katmanlar")]
        [Tooltip("Nişan ve zemin için sayılan katmanlar. Oyuncunun katmanı hariç tutulmalı.")]
        [SerializeField] private LayerMask groundMask = ~0;
        [Tooltip("Yer dolu mu diye bakılan katmanlar (oyuncu dahil: yankı oyuncunun üstüne koymasın).")]
        [SerializeField] private LayerMask overlapMask = ~0;
        [Tooltip("Taşıyanın kendi collider'ı (varsa). Ona çok yakın yerleştirme geçersiz sayılır.")]
        [SerializeField] private Collider ownBody;

        // Eldeki eşya değişince (alma, bırakma, verme). Null = el boş. Örnek: ağır eşya oyuncuyu yavaşlatır.
        public event Action<Pickup> HeldItemChanged;

        public Pickup HeldItem { get; private set; }

        private readonly Collider[] overlapBuffer = new Collider[8];

        public bool IsHolding(ItemDefinition item)
        {
            return HeldItem != null && item != null && HeldItem.Definition == item;
        }

        public void Hold(Pickup item)
        {
            item.AttachTo(holdPoint);
            SetHeld(item);
        }

        // Eldeki eşyayı başka bir sisteme verir (örnek: geçiş kutusu). Eşyayı nereye koyacağı alana kalır.
        public Pickup Take()
        {
            Pickup item = HeldItem;
            SetHeld(null);
            return item;
        }

        // Nişan alınan yere göre yerleştirme pozu. Zemin bulunamazsa false.
        // valid: o poz şu an boş mu (önizleme yeşil/kırmızı).
        public bool TryGetPlacement(out Pose pose, out bool valid)
        {
            pose = default;
            valid = false;

            if (HeldItem == null)
            {
                return false;
            }

            Vector3 extents = HeldItem.Extents;
            float yaw = dropOrigin.eulerAngles.y;
            if (yawSnap > 0f)
            {
                yaw = Mathf.Round(yaw / yawSnap) * yawSnap;
            }

            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 origin = dropOrigin.position;
            Vector3 direction = dropOrigin.forward;
            Vector3 point;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, placeRange, groundMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.normal.y >= floorNormalThreshold)
                {
                    // Zemine ya da bir eşyanın üstüne bakılıyor: doğrudan üstüne.
                    pose = new Pose(hit.point + Vector3.up * (extents.y + gap), rotation);
                    valid = !IsBlocked(pose, extents);
                    return true;
                }

                // Dik yüzey (duvar, kasa yanı): yüzeyin önüne, sonra zemine indir.
                Vector3 awayFromWall = Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized;
                point = hit.point + awayFromWall * (Mathf.Max(extents.x, extents.z) + gap);
            }
            else
            {
                point = origin + direction * placeRange;
            }

            if (!Physics.Raycast(point, Vector3.down, out RaycastHit ground, groundSearchDepth, groundMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            pose = new Pose(ground.point + Vector3.up * (extents.y + gap), rotation);
            valid = !IsBlocked(pose, extents);
            return true;
        }

        // Oyuncunun bırakması: önizlemedeki poza koyar. Geçersizse koymaz.
        public bool Drop(out Pose placed)
        {
            placed = default;
            return TryGetPlacement(out placed, out bool valid) && valid && PlaceAt(placed);
        }

        // Verilen poza koyar (yankı kayıttaki pozu kullanır). Yer doluysa koymaz.
        public bool PlaceAt(Pose pose)
        {
            if (HeldItem == null || IsBlocked(pose, HeldItem.Extents))
            {
                return false;
            }

            Pickup item = HeldItem;
            SetHeld(null);
            item.Release(pose.position, pose.rotation);
            return true;
        }

        private void SetHeld(Pickup item)
        {
            if (HeldItem == item)
            {
                return;
            }

            HeldItem = item;
            HeldItemChanged?.Invoke(item);
        }

        // Eşya elde tutulurken collider'ları kapalı olduğu için kendisiyle çakışmaz.
        private bool IsBlocked(Pose pose, Vector3 extents)
        {
            int count = Physics.OverlapBoxNonAlloc(pose.position, extents * 0.98f, overlapBuffer, pose.rotation,
                overlapMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (overlapBuffer[i] != ownBody)
                {
                    return true;
                }
            }

            // Taşıyanın gövdesine fazla yakın mı? (skin width dahil gövdeye değmesin, üstüne çıkılmasın)
            if (ownBody != null)
            {
                Vector3 padded = extents + Vector3.one * bodyMargin;
                count = Physics.OverlapBoxNonAlloc(pose.position, padded, overlapBuffer, pose.rotation,
                    overlapMask, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    if (overlapBuffer[i] == ownBody)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
