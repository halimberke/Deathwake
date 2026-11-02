using UnityEngine;

namespace Game.Audio
{
    // Yürünen mesafeye göre adım sesi ve yere iniş sesi. Girdiye bakmaz: konum değişimini izler,
    // bu yüzden oyuncu da yankı da aynı bileşeni kullanır. Hızlandırılmış zamanda da doğru çalışır.
    public class Footsteps : MonoBehaviour
    {
        [SerializeField] private FootstepBank bank;
        [Tooltip("İki adım arası yürünen mesafe (m).")]
        [SerializeField] private float stepLength = 1.9f;
        [Tooltip("Zemin aranırken ayağın bu kadar üstünden başlanır.")]
        [SerializeField] private float probeHeight = 0.3f;
        [Tooltip("Ayağın altında bu kadar derine bakılır.")]
        [SerializeField] private float probeDepth = 0.25f;
        [Tooltip("Havada en az bu kadar kalınca iniş sesi çalar (sn).")]
        [SerializeField] private float minAirTime = 0.25f;
        [Tooltip("Bir karede bundan fazla yer değişirse ışınlanmadır, adım sayılmaz.")]
        [SerializeField] private float teleportDistance = 1.5f;
        [SerializeField] private LayerMask groundMask = ~0;

        private Vector3 lastPosition;
        private float walked;
        private float airTime;

        private void OnEnable()
        {
            lastPosition = transform.position;
            walked = stepLength * 0.5f; // İlk adım hareket başlar başlamaz gelsin.
            airTime = 0f;
        }

        private void Update()
        {
            Vector3 position = transform.position;
            Vector3 moved = position - lastPosition;
            moved.y = 0f;
            lastPosition = position;

            float distance = moved.magnitude;
            if (distance > teleportDistance)
            {
                return;
            }

            // CharacterController.isGrounded titrek olduğu için zemin ayrı aranır.
            Vector3 origin = position + Vector3.up * probeHeight;
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit ground, probeHeight + probeDepth,
                    groundMask, QueryTriggerInteraction.Ignore))
            {
                airTime += Time.deltaTime;
                return;
            }

            if (airTime >= minAirTime)
            {
                Sfx.Play(bank.Find(ground.collider).Land, position);
                walked = 0f;
            }

            airTime = 0f;
            walked += distance;
            if (walked >= stepLength)
            {
                walked -= stepLength;
                Sfx.Play(bank.Find(ground.collider).Steps, position);
            }
        }
    }
}
