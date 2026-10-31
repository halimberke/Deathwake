using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    // Açma/kapama anahtarı: başlık yuvada kayar, yuva vurgu rengine döner.
    [RequireComponent(typeof(Button))]
    public class ToggleSwitch : MonoBehaviour
    {
        [SerializeField] private UITheme theme;
        [SerializeField] private Image track;
        [SerializeField] private RectTransform knob;
        [Tooltip("Başlığın merkezden sağa/sola uzaklığı.")]
        [SerializeField] private float travel = 16f;

        public bool Value { get; private set; }

        public event Action<bool> Changed;

        private float amount;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Toggle);
            GetComponent<Button>().onClick.AddListener(UIAudio.Click);
        }

        public void SetValueWithoutNotify(bool value)
        {
            Value = value;
            amount = value ? 1f : 0f;
            Apply();
        }

        private void Toggle()
        {
            Value = !Value;
            Changed?.Invoke(Value);
        }

        private void Update()
        {
            float target = Value ? 1f : 0f;
            if (!Mathf.Approximately(amount, target))
            {
                amount = Mathf.MoveTowards(amount, target, theme.HoverSpeed * Time.unscaledDeltaTime);
                Apply();
            }
        }

        private void Apply()
        {
            float eased = UITheme.EaseOut(amount);
            knob.anchoredPosition = new Vector2(Mathf.Lerp(-travel, travel, eased), 0f);
            track.color = Color.Lerp(theme.Control, theme.Accent, eased);
        }
    }
}
