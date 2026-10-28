using System.Collections;
using TMPro;
using UnityEngine;

namespace Game.Narrative
{
    // Ekranın altındaki altyazı kutusu: üstte konuşanın adı, altta satır.
    // Satır baştan tam yerleşir (kutu boyutu oynamasın), harfler sesle birlikte görünür olur.
    [RequireComponent(typeof(CanvasGroup))]
    public class SubtitleUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text speakerLabel;
        [SerializeField] private TMP_Text lineLabel;
        [SerializeField] private float fadeDuration = 0.25f;

        private CanvasGroup group;
        private Coroutine fade;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
        }

        public void Show(string speaker, Color speakerColor, string line)
        {
            speakerLabel.text = speaker;
            speakerLabel.color = speakerColor;
            lineLabel.text = line;
            lineLabel.maxVisibleCharacters = 0;
            FadeTo(1f);
        }

        public void SetVisibleCharacters(int count)
        {
            lineLabel.maxVisibleCharacters = count;
        }

        public void Hide()
        {
            FadeTo(0f);
        }

        private void FadeTo(float target)
        {
            if (fade != null)
            {
                StopCoroutine(fade);
            }

            fade = StartCoroutine(FadeRoutine(target));
        }

        // Duraklatma ve hızlandırmadan etkilenmesin diye ölçeksiz zaman.
        private IEnumerator FadeRoutine(float target)
        {
            float start = group.alpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
                yield return null;
            }

            group.alpha = target;
            fade = null;
        }
    }
}
