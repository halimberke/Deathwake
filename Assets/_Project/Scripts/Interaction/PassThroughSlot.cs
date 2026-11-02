using Game.Audio;
using Game.Localization;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Interaction
{
    // Geçiş kutusunun bir yüzü. Elindeki eşyayı kutunun öbür tarafına geçirir.
    // Eşya sığar, insan sığmaz: duvarın ardına bir şey vermenin tek yolu.
    public class PassThroughSlot : MonoBehaviour, IInteractable
    {
        [Tooltip("Eşyanın çıkacağı nokta (kutunun öbür tarafı).")]
        [SerializeField] private Transform outputPoint;
        [SerializeField] private string prompt = "Kutuya koy";
        [SerializeField] private string emptyHandPrompt = "Geçiş kutusu";
        [SerializeField] private SoundSet useSound;
        [Tooltip("Kutuyu yankı kullandığında (oyuncu değil). Örnek: karakterin şaşkınlığı.")]
        [SerializeField] private UnityEvent onUsedByEcho;

        public string GetPrompt(IInteractor interactor)
        {
            return CanInteract(interactor)
                ? $"{Loc.T(prompt)} ({interactor.Hand.HeldItem.DisplayName})"
                : Loc.T(emptyHandPrompt);
        }

        public bool CanInteract(IInteractor interactor)
        {
            return interactor.Hand != null && interactor.Hand.HeldItem != null;
        }

        public void Interact(IInteractor interactor)
        {
            Sfx.Play(useSound, transform.position);
            if (!(interactor is PlayerInteractor))
            {
                onUsedByEcho.Invoke();
            }

            Pickup item = interactor.Hand.Take();
            item.Release(outputPoint.position, outputPoint.rotation);
        }
    }
}
