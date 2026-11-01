using System;
using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Settings
{
    // Tuş listesinin bir satırı: eylemin adı ve tuşu gösteren çerçeveli etiket.
    public class KeybindRow : MonoBehaviour
    {
        [SerializeField] private UITheme theme;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text keyText;
        [SerializeField] private Button rebindButton;
        [Tooltip("Tuş etiketinin çerçevesi. Seçilince ve tuş beklenirken vurgulanır.")]
        [SerializeField] private Image keyFrame;
        [Tooltip("Tuş beklenirken saniyedeki nabız.")]
        [SerializeField] private float pulseRate = 3f;

        public KeybindCatalog.Entry Entry { get; private set; }
        public int BindingIndex { get; private set; }
        public Button Button => rebindButton;

        public InputAction Action => Entry.Action;

        private bool waiting;

        public void Init(KeybindCatalog.Entry entry, Action<KeybindRow> onRebind)
        {
            Entry = entry;
            BindingIndex = entry.FindBindingIndex();
            rebindButton.onClick.AddListener(() => onRebind(this));
            rebindButton.onClick.AddListener(UIAudio.Click);
            Refresh();
        }

        public void Refresh()
        {
            waiting = false;
            labelText.text = Entry.Label;
            keyText.color = theme.TextPrimary;
            keyText.text = BindingIndex >= 0
                ? Action.GetBindingDisplayString(BindingIndex, InputBinding.DisplayStringOptions.DontIncludeInteractions)
                : "-";
        }

        public void ShowWaiting(string text)
        {
            waiting = true;
            keyText.text = text;
            keyText.color = theme.Accent;
        }

        private void Update()
        {
            bool focused = UIFocus.Current == rebindButton.gameObject;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseRate * Mathf.PI * 2f);
            Color frame = waiting
                ? Color.Lerp(theme.Control, theme.Accent, pulse)
                : focused ? theme.Accent : theme.Control;
            keyFrame.color = frame;
        }
    }
}
