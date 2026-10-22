using UnityEngine;
using UnityEngine.UI;

namespace Game.Player
{
    // Can azaldıkça ekran kenarlarını kırmızılaştırır; her vuruşta kısa bir parlama.
    // Ayrı bir can çubuğu yok: oyuncu tehlikeyi ekranın kendisinden okur.
    public class DamageVignette : MonoBehaviour
    {
        [SerializeField] private PlayerHealth health;
        [SerializeField] private Image overlay;
        [Tooltip("Can sıfıra yaklaşırken ulaşılan en yüksek opaklık.")]
        [SerializeField] private float maxAlpha = 0.6f;
        [Tooltip("Vuruş anındaki ek parlama.")]
        [SerializeField] private float hitFlash = 0.25f;
        [SerializeField] private float flashFadeSpeed = 3f;

        private float flash;

        private void OnEnable()
        {
            health.Damaged += HandleDamaged;
        }

        private void OnDisable()
        {
            health.Damaged -= HandleDamaged;
        }

        private void HandleDamaged()
        {
            flash = hitFlash;
        }

        private void Update()
        {
            flash = Mathf.Max(0f, flash - flashFadeSpeed * Time.unscaledDeltaTime);
            Color color = overlay.color;
            color.a = Mathf.Clamp01((1f - health.Normalized) * maxAlpha + flash);
            overlay.color = color;
        }
    }
}
