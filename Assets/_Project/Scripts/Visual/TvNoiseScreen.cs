using UnityEngine;

namespace Game.Visual
{
    // Karlı televizyon ekranı: gürültü dokusunu rastgele kaydırır, odaya vuran ışığı hafifçe oynatır.
    public class TvNoiseScreen : MonoBehaviour
    {
        private static readonly int MapStId = Shader.PropertyToID("_BaseMap_ST");
        private static readonly int EmissionId = Shader.PropertyToID("_Emission");

        [SerializeField] private Renderer screen;
        [SerializeField] private Light roomLight;
        [SerializeField] private Vector2 tiling = new Vector2(0.5f, 0.5f);
        [SerializeField] private Color glow = new Color(0.34f, 0.37f, 0.42f);
        [Tooltip("Saniyedeki kare sayısı. Düşük olması eski televizyon gibi görünür.")]
        [SerializeField] private float frameRate = 18f;
        [SerializeField] private Vector2 lightRange = new Vector2(0.75f, 1.05f);

        private MaterialPropertyBlock block;
        private float baseIntensity;
        private float nextFrame;
        private bool isOn = true;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            baseIntensity = roomLight.intensity;
        }

        private void Update()
        {
            if (!isOn || Time.time < nextFrame)
            {
                return;
            }

            nextFrame = Time.time + 1f / frameRate;
            Apply(new Vector4(tiling.x, tiling.y, Random.value, Random.value), glow);
            roomLight.intensity = baseIntensity * Random.Range(lightRange.x, lightRange.y);
        }

        public void TurnOff()
        {
            isOn = false;
            Apply(new Vector4(tiling.x, tiling.y, 0f, 0f), Color.black);
        }

        private void Apply(Vector4 mapSt, Color emission)
        {
            screen.GetPropertyBlock(block);
            block.SetVector(MapStId, mapSt);
            block.SetColor(EmissionId, emission);
            screen.SetPropertyBlock(block);
        }
    }
}
