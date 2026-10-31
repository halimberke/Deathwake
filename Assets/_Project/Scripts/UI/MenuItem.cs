using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    // Yazı biçimli menü öğesi. Seçilince sağa kayar, yanında vurgu çizgisi ve solan parlama belirir.
    // Fareyle üzerine gelmek de klavyeyle seçmek de aynı durumdur.
    [RequireComponent(typeof(Button))]
    public class MenuItem : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private UITheme theme;
        [SerializeField] private TMP_Text label;
        [Tooltip("Seçilince kayan parça: yazı burada durur.")]
        [SerializeField] private RectTransform content;
        [Tooltip("Sol kenardaki ince vurgu çizgisi.")]
        [SerializeField] private Graphic bar;
        [Tooltip("Arkadaki solan parlama.")]
        [SerializeField] private Graphic glow;
        [SerializeField] private float glowAlpha = 0.12f;
        [SerializeField] private float hoverOffset = 14f;
        [SerializeField] private float introOffset = -22f;
        [Tooltip("Geri alınamayan eylem: vurgu rengi kırmızı olur.")]
        [SerializeField] private bool danger;

        public TMP_Text Label => label;
        public Button Button => button != null ? button : button = GetComponent<Button>();

        private Button button;
        private CanvasGroup group;
        private Vector2 contentRest;
        private bool restCaptured;
        private bool selected;
        private bool tabActive;
        private float amount;
        private float introClock = 1f;
        private float introDelay;
        private bool introPlaying;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            CaptureRest();
            Button.onClick.AddListener(UIAudio.Click);
        }

        private void OnEnable()
        {
            amount = selected || tabActive ? 1f : 0f;
            Apply();
        }

        private void OnDisable()
        {
            selected = false;
            introPlaying = false;
            if (group != null)
            {
                group.alpha = 1f;
            }
        }

        private void CaptureRest()
        {
            if (!restCaptured && content != null)
            {
                contentRest = content.anchoredPosition;
                restCaptured = true;
            }
        }

        // Sekme olarak kullanılırken: seçili sekme kalıcı olarak vurgulu kalır.
        public void SetTabActive(bool value)
        {
            tabActive = value;
        }

        public void PlayIntro(float delay)
        {
            CaptureRest();
            introDelay = delay;
            introClock = 0f;
            introPlaying = true;
            if (group != null)
            {
                group.alpha = 0f;
            }

            Apply();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Button.IsInteractable())
            {
                UIFocus.Hover(gameObject);
            }
        }

        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
            UIAudio.Hover();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
        }

        private void Update()
        {
            float target = selected || tabActive ? 1f : 0f;
            bool changed = !Mathf.Approximately(amount, target);
            amount = Mathf.MoveTowards(amount, target, theme.HoverSpeed * Time.unscaledDeltaTime);

            if (introPlaying)
            {
                introClock += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01((introClock - introDelay) / theme.ItemIntroDuration);
                if (group != null)
                {
                    group.alpha = k;
                }

                introPlaying = k < 1f;
                changed = true;
            }

            if (changed)
            {
                Apply();
            }
        }

        private void Apply()
        {
            float eased = UITheme.EaseOut(amount);
            Color accent = danger ? theme.Danger : theme.Accent;

            float intro = introPlaying
                ? Mathf.Lerp(introOffset, 0f, UITheme.EaseOut((introClock - introDelay) / theme.ItemIntroDuration))
                : 0f;
            if (content != null)
            {
                content.anchoredPosition = contentRest + Vector2.right * (eased * hoverOffset + intro);
            }

            if (label != null)
            {
                Color idle = Button.interactable ? theme.TextMuted : theme.TextDisabled;
                Color active = danger ? theme.Danger : theme.TextPrimary;
                label.color = Color.Lerp(idle, active, eased);
            }

            if (bar != null)
            {
                bar.color = new Color(accent.r, accent.g, accent.b, eased);
                bar.rectTransform.localScale = new Vector3(1f, eased, 1f);
            }

            if (glow != null)
            {
                glow.color = new Color(accent.r, accent.g, accent.b, eased * glowAlpha);
            }
        }
    }
}
