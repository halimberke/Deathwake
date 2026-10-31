using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI
{
    // EventSystem seçimi için kısayollar. Fare ve klavye aynı seçimi kullanır:
    // üzerine gelinen öğe seçili olur, oklarla gezilince de aynı vurgu görünür.
    public static class UIFocus
    {
        public static GameObject Current =>
            EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        // Sessiz seçim: panel açılınca ilk öğeye odaklanmak gibi.
        public static void Select(GameObject target)
        {
            if (EventSystem.current == null)
            {
                return;
            }

            UIAudio.SilenceThisFrame();

            // Panel kapanıp açılınca seçim aynı nesnede kalır; önce boşaltılmazsa OnSelect çalışmaz.
            if (EventSystem.current.currentSelectedGameObject == target)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            EventSystem.current.SetSelectedGameObject(target);
        }

        // Kullanıcı seçimi: üzerine gelme. Ses çıkarır.
        public static void Hover(GameObject target)
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != target)
            {
                EventSystem.current.SetSelectedGameObject(target);
            }
        }

        // Tuş atanırken ok tuşları ve WASD arayüzde gezinmesin.
        public static void SetNavigation(bool enabled)
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.sendNavigationEvents = enabled;
            }
        }
    }
}
