using System;
using Game.Localization;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    // Onay penceresi: geri alınamayan eylemlerden önce sorar.
    // Açıkken alttaki panele tıklanamaz; kapanınca odak oraya döner.
    public class ConfirmDialog : MonoBehaviour
    {
        [SerializeField] private UIPanel panel;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField] private MenuItem confirmItem;
        [SerializeField] private MenuItem cancelItem;

        public bool IsOpen => panel.IsOpen;

        private UIPanel blocked;
        private Action onConfirm;

        private void Awake()
        {
            confirmItem.Button.onClick.AddListener(Confirm);
            cancelItem.Button.onClick.AddListener(Cancel);
        }

        public void Ask(string title, string body, string confirmText, UIPanel blockedPanel, Action confirmed)
        {
            titleLabel.text = Loc.T(title);
            bodyLabel.text = Loc.T(body);
            confirmItem.Label.text = Loc.T(confirmText);
            onConfirm = confirmed;
            blocked = blockedPanel;
            blocked.SetInteractable(false);
            // Varsayılan seçim iptal: Enter'a basıp yanlışlıkla silmek olmasın.
            panel.Show(cancelItem.Button);
        }

        public void Cancel()
        {
            Close();
        }

        private void Confirm()
        {
            Action action = onConfirm;
            Close();
            action?.Invoke();
        }

        private void Close()
        {
            panel.Hide();
            if (blocked != null)
            {
                blocked.SetInteractable(true);
                blocked = null;
            }
        }
    }
}
