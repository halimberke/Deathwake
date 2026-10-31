using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    // Ayarlar satırı: üzerine gelince ya da içindeki kontrol seçiliyken arkası hafifçe aydınlanır.
    public class SettingRow : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] private UITheme theme;
        [Tooltip("Satırın arka planı. Fare olaylarını da bu yakalar.")]
        [SerializeField] private Graphic background;
        [Tooltip("Üzerine gelince seçilen kontrol.")]
        [SerializeField] private Selectable control;

        private float amount;

        public void OnPointerEnter(PointerEventData eventData)
        {
            GameObject current = UIFocus.Current;
            bool alreadyInside = current != null && current.transform.IsChildOf(transform);
            if (!alreadyInside && control != null && control.IsInteractable())
            {
                UIFocus.Hover(control.gameObject);
                UIAudio.Hover();
            }
        }

        private void OnEnable()
        {
            amount = 0f;
            Apply();
        }

        private void Update()
        {
            GameObject current = UIFocus.Current;
            float target = current != null && current.transform.IsChildOf(transform) ? 1f : 0f;
            if (!Mathf.Approximately(amount, target))
            {
                amount = Mathf.MoveTowards(amount, target, theme.HoverSpeed * Time.unscaledDeltaTime);
                Apply();
            }
        }

        private void Apply()
        {
            Color color = theme.RowHighlight;
            color.a *= amount;
            background.color = color;
        }
    }
}
