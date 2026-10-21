using UnityEngine;

namespace Game.Chamber
{
    // Belli bir zamandan sonra yerden yükselen gaz. Oyuncunun göz hizasına ulaşınca öldürür.
    // Görsel bir kutudur: yüksekliği oda saatine göre büyür.
    public class RisingGasHazard : MonoBehaviour
    {
        [SerializeField] private ChamberController chamber;
        [Tooltip("Ölçeği değişecek gaz kutusu. Pivotu ortada olan bir küp (varsayılan Cube).")]
        [SerializeField] private Transform gasVolume;
        [Tooltip("Oyuncu bu etiketle bulunur (oyuncu oda sahnesinde değil, Core sahnesinde).")]
        [SerializeField] private string playerTag = "Player";

        [Header("Zamanlama (oda saati, saniye)")]
        [SerializeField] private float riseStartTime = 25f;
        [SerializeField] private float riseEndTime = 45f;

        [Header("Yükseklik")]
        [Tooltip("Gazın zeminden itibaren ulaşacağı en yüksek nokta.")]
        [SerializeField] private float maxHeight = 2.2f;
        [Tooltip("Oyuncunun ayağından göz hizasına mesafe.")]
        [SerializeField] private float playerEyeHeight = 1.6f;
        [SerializeField] private string deathReason = "Gazdan boğuldun";

        // Yerel koordinatta: oda yüklendikten sonra başka bir yere taşınsa da geçerli kalır.
        private float floorLocalY;
        private Transform player;

        public float CurrentHeight { get; private set; }

        private void Awake()
        {
            floorLocalY = gasVolume.localPosition.y - gasVolume.localScale.y * 0.5f;
            SetHeight(0f);
        }

        private void OnEnable()
        {
            chamber.StateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            chamber.StateChanged -= HandleStateChanged;
        }

        // Oyuncu deneme başında bir kez aranır; Update içinde arama yapılmaz.
        private void HandleStateChanged(ChamberState previous, ChamberState next)
        {
            if (next == ChamberState.Running && player == null)
            {
                GameObject found = GameObject.FindWithTag(playerTag);
                player = found != null ? found.transform : null;
            }
        }

        // Gazın yüksekliği her karede saatten hesaplanır (zaman kontrolü değil, görselin kendisi).
        // Öldürme kararı da bu yükseklikten türer; ayrı bir sabit zaman yazılmaz.
        private void Update()
        {
            if (chamber.State != ChamberState.Running || player == null)
            {
                return;
            }

            float progress = Mathf.InverseLerp(riseStartTime, riseEndTime, chamber.Clock.Time);
            SetHeight(progress * maxHeight);

            float gasTopY = gasVolume.parent.TransformPoint(0f, floorLocalY + CurrentHeight, 0f).y;
            if (gasTopY >= player.position.y + playerEyeHeight)
            {
                chamber.Kill(deathReason);
            }
        }

        private void SetHeight(float height)
        {
            CurrentHeight = height;
            gasVolume.gameObject.SetActive(height > 0.001f);

            Vector3 scale = gasVolume.localScale;
            scale.y = Mathf.Max(height, 0.001f);
            gasVolume.localScale = scale;

            Vector3 position = gasVolume.localPosition;
            position.y = floorLocalY + scale.y * 0.5f;
            gasVolume.localPosition = position;
        }
    }
}
