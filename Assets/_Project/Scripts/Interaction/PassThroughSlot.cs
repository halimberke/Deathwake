using Game.Localization;
using UnityEngine;

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
            Pickup item = interactor.Hand.Take();
            item.Release(outputPoint.position, outputPoint.rotation);
        }
    }
}
