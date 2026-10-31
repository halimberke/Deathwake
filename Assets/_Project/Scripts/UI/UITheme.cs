using UnityEngine;

namespace Game.UI
{
    // Arayüzün tek görsel kaynağı: renkler ve hareket süreleri.
    // Bir rengi burada değiştirmek bütün menülere yansır.
    [CreateAssetMenu(fileName = "UITheme", menuName = "Game/UI Theme")]
    public class UITheme : ScriptableObject
    {
        [Header("Renk")]
        [SerializeField] private Color textPrimary = new Color(0.93f, 0.95f, 0.96f);
        [SerializeField] private Color textMuted = new Color(0.62f, 0.66f, 0.70f);
        [SerializeField] private Color textDisabled = new Color(0.34f, 0.37f, 0.40f);
        [Tooltip("Tek vurgu rengi: yankının mavisi.")]
        [SerializeField] private Color accent = new Color(0.45f, 0.85f, 1f);
        [Tooltip("Geri alınamayan eylemler.")]
        [SerializeField] private Color danger = new Color(1f, 0.42f, 0.36f);
        [SerializeField] private Color rowHighlight = new Color(1f, 1f, 1f, 0.045f);
        [SerializeField] private Color control = new Color(1f, 1f, 1f, 0.16f);

        [Header("Hareket")]
        [Tooltip("Üzerine gelme geçişi: saniyede tamamlanan oran.")]
        [SerializeField] private float hoverSpeed = 12f;
        [SerializeField] private float panelDuration = 0.22f;
        [Tooltip("Panel açılırken kaydığı mesafe.")]
        [SerializeField] private float panelSlide = 28f;
        [Tooltip("Menü öğelerinin sırayla belirmesindeki gecikme.")]
        [SerializeField] private float staggerDelay = 0.045f;
        [SerializeField] private float itemIntroDuration = 0.26f;

        public Color TextPrimary => textPrimary;
        public Color TextMuted => textMuted;
        public Color TextDisabled => textDisabled;
        public Color Accent => accent;
        public Color Danger => danger;
        public Color RowHighlight => rowHighlight;
        public Color Control => control;
        public float HoverSpeed => hoverSpeed;
        public float PanelDuration => panelDuration;
        public float PanelSlide => panelSlide;
        public float StaggerDelay => staggerDelay;
        public float ItemIntroDuration => itemIntroDuration;

        // Geçişlerde kullanılan yumuşak yavaşlama eğrisi.
        public static float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            float inverse = 1f - t;
            return 1f - inverse * inverse * inverse;
        }
    }
}
