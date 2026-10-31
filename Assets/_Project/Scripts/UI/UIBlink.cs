using UnityEngine;

namespace Game.UI
{
    // Yavaş nefes alan saydamlık: "gözlem aktif" noktası gibi durum göstergeleri için.
    [RequireComponent(typeof(CanvasGroup))]
    public class UIBlink : MonoBehaviour
    {
        [SerializeField] private float period = 2.4f;
        [SerializeField] private float minAlpha = 0.25f;

        private CanvasGroup group;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / period);
            group.alpha = Mathf.Lerp(minAlpha, 1f, wave);
        }
    }
}
