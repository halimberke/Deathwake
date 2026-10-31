using UnityEngine;

namespace Game.UI
{
    // Menü sahnesindeki yankı: koridorda kameraya doğru yürür, yaklaşınca dağılır, uzakta yeniden belirir.
    // Kaydı oynatılan bir yankı değil, sadece görsel. Yürüyüş yankının shader'ını kullanır.
    public class MenuEchoWalker : MonoBehaviour
    {
        [SerializeField] private MeshRenderer figure;
        [SerializeField] private MeshFilter figureMesh;
        [Tooltip("Adım pozları. Sırayla değişir: yumuşak geçiş yok, PS1 tarzı kesik yürüyüş.")]
        [SerializeField] private Mesh[] stepPoses;
        [Tooltip("Yürüyüşün başladığı ve dağıldığı yerel z.")]
        [SerializeField] private float startZ = 24f;
        [SerializeField] private float endZ = 3f;
        [SerializeField] private float speed = 1.15f;
        [Tooltip("Başlangıçtaki ilerleme (0-1). Yankılar sıralı dursun diye her birine farklı verilir.")]
        [Range(0f, 1f)]
        [SerializeField] private float startProgress;
        [SerializeField] private float laneWidth = 0.9f;
        [SerializeField] private float materializeDistance = 2f;
        [SerializeField] private float bobHeight = 0.035f;
        [SerializeField] private float stepPeriod = 0.62f;

        private static readonly int MaterializeId = Shader.PropertyToID("_Materialize");
        private static readonly int GlowId = Shader.PropertyToID("_Glow");
        private static readonly int BaseHeightId = Shader.PropertyToID("_BaseHeight");

        private MaterialPropertyBlock block;
        private float lane;
        private float distance;
        private Vector3 origin;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            origin = transform.localPosition;
            distance = (startZ - endZ) * startProgress;
            PickLane();
        }

        private void PickLane()
        {
            lane = Random.Range(-laneWidth, laneWidth);
        }

        private void Update()
        {
            float length = startZ - endZ;
            distance += speed * Time.deltaTime;
            if (distance >= length)
            {
                distance -= length;
                PickLane();
            }

            float bob = Mathf.Abs(Mathf.Sin(distance / speed * Mathf.PI / stepPeriod)) * bobHeight;
            transform.localPosition = new Vector3(origin.x + lane, origin.y + bob, startZ - distance);

            int step = Mathf.FloorToInt(distance / speed / stepPeriod) % stepPoses.Length;
            figureMesh.sharedMesh = stepPoses[step];

            float appear = Mathf.Clamp01(distance / materializeDistance);
            float vanish = Mathf.Clamp01((length - distance) / materializeDistance);

            block.SetFloat(MaterializeId, Mathf.Min(appear, vanish));
            block.SetFloat(GlowId, 0f);
            block.SetFloat(BaseHeightId, transform.position.y);
            figure.SetPropertyBlock(block);
        }
    }
}
