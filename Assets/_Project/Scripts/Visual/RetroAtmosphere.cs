using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Game.Visual
{
    // Sis ve ortam ışığını merkezden yönetir. Unity'de bu ayarlar aktif sahneye aittir;
    // odalar yüklendikçe aktif sahne değiştiği için her değişimde aynı atmosfer yeniden uygulanır.
    public class RetroAtmosphere : MonoBehaviour
    {
        [Header("Sis")]
        [SerializeField] private bool fog = true;
        [SerializeField] private Color fogColor = new Color(0.02f, 0.025f, 0.03f);
        [Tooltip("Sisin başladığı mesafe (m).")]
        [SerializeField] private float fogStart = 6f;
        [Tooltip("Her şeyin tamamen sise gömüldüğü mesafe (m).")]
        [SerializeField] private float fogEnd = 30f;

        [Header("Ortam ışığı")]
        [Tooltip("Hiçbir lambanın aydınlatmadığı yerlerin rengi. Karanlık ama simsiyah değil.")]
        [SerializeField] private Color ambientColor = new Color(0.35f, 0.36f, 0.4f);
        [Tooltip("Core'daki genel (yön) ışık. Gücünü her sahne kendisi belirler.")]
        [SerializeField] private Light sun;

        [Header("Gaz")]
        [Tooltip("Oda gazla dolunca sisin başlangıç ve bitiş mesafesi (m).")]
        [SerializeField] private float gasFogStart = 0.3f;
        [SerializeField] private float gasFogEnd = 4f;

        private float gasAmount;
        private Color gasColor;

        // 0 = temiz hava, 1 = oda gazla dolu. Sis yaklaşır ve gaz rengine döner.
        public void SetGas(float amount, Color color)
        {
            gasAmount = Mathf.Clamp01(amount);
            gasColor = color;
            ApplyFog();
        }

        // Sahneye girilince oyun akışı çağırır: o sahnenin genel ışığı ve ortam rengi.
        public void SetRoomLighting(float sunIntensity, Color ambient)
        {
            ambientColor = ambient;
            if (sun != null)
            {
                sun.intensity = sunIntensity;
                sun.enabled = sunIntensity > 0f;
            }

            Apply();
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
            Apply();
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        }

        private void HandleActiveSceneChanged(Scene previous, Scene next)
        {
            Apply();
        }

        private void Apply()
        {
            ApplyFog();

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor;

            // Shader'lar ortam ışığını bu yoklamadan okur; renk değişince kendiliğinden güncellenmeyebilir.
            var probe = new SphericalHarmonicsL2();
            probe.AddAmbientLight(ambientColor);
            RenderSettings.ambientProbe = probe;
        }

        private void ApplyFog()
        {
            // Yumuşak başlangıç: gaz ilk saniyelerde uzakta belirir, sonra hızla yaklaşır.
            float k = gasAmount * gasAmount;
            RenderSettings.fog = fog;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Color.Lerp(fogColor, gasColor, k);
            RenderSettings.fogStartDistance = Mathf.Lerp(fogStart, gasFogStart, k);
            RenderSettings.fogEndDistance = Mathf.Lerp(fogEnd, gasFogEnd, k);
        }

#if UNITY_EDITOR
        // Inspector'da değer değişince Play sırasında hemen görülsün.
        private void OnValidate()
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                Apply();
            }
        }
#endif
    }
}
