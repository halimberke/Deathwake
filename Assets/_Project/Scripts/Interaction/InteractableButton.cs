using UnityEngine;
using UnityEngine.Events;

namespace Game.Interaction
{
    // Basılınca bir olay tetikleyen düğme, şalter ya da pano.
    // Ne olacağı inspector'daki On Pressed listesinden bağlanır.
    public class InteractableButton : MonoBehaviour, IInteractable
    {
        [SerializeField] private string prompt = "Bas";
        [Tooltip("Kapalıyken ekranda bu yazı görünür ve düğme çalışmaz.")]
        [SerializeField] private string disabledPrompt = "Çalışmıyor";
        [SerializeField] private bool isEnabled = true;
        [Tooltip("Açıksa düğme sadece bir kez çalışır.")]
        [SerializeField] private bool singleUse;

        [SerializeField] private UnityEvent onPressed;

        private bool used;

        public string GetPrompt(IInteractor interactor)
        {
            return CanInteract(interactor) ? prompt : disabledPrompt;
        }

        public bool CanInteract(IInteractor interactor)
        {
            return isEnabled && !(singleUse && used);
        }

        public void Interact(IInteractor interactor)
        {
            used = true;
            onPressed.Invoke();
        }

        // Başka bir nesneden (örnek: sigorta) açıp kapamak için. UnityEvent'ten de çağrılabilir.
        public void SetEnabled(bool value)
        {
            isEnabled = value;
        }

        // Test için: olaya bağlanıp konsola yazar.
        public void LogPressed()
        {
            Debug.Log($"{name} basıldı.", this);
        }
    }
}
