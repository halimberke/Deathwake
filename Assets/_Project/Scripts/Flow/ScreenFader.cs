using System.Collections;
using UnityEngine;

namespace Game.Flow
{
    // Ekranı karartıp açar. Ölüm ve yeniden doğma anını gizler.
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] private float duration = 0.35f;

        private CanvasGroup group;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        public IEnumerator FadeOut() => FadeTo(1f);

        public IEnumerator FadeIn() => FadeTo(0f);

        // Zaman hızlandırılmış ya da durmuş olsa bile aynı sürede çalışsın diye ölçeksiz zaman kullanılır.
        private IEnumerator FadeTo(float target)
        {
            float start = group.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }

            group.alpha = target;
        }
    }
}
