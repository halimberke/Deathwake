using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Interaction
{
    // Ekran ortasındaki nokta ve altındaki "E: ..." yazısı.
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private Image crosshair;
        [SerializeField] private TMP_Text promptText;

        [Header("Görünüm")]
        [SerializeField] private string keyLabel = "E";
        [SerializeField] private string dropPrompt = "Bırak";
        [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color blockedColor = new Color(1f, 0.4f, 0.4f, 1f);

        private void Update()
        {
            // Yazı her karede okunur: kapı açılınca "Aç" yerine "Kapat" yazması gibi
            // hedef aynı kalsa da değişebilir. Tek bir arayüz çağrısı, maliyeti düşük.
            IInteractable target = interactor.CurrentTarget;

            if (target == null)
            {
                crosshair.color = idleColor;
                promptText.text = interactor.CanDrop
                    ? $"{keyLabel}: {dropPrompt} ({interactor.Hand.HeldItem.DisplayName})"
                    : string.Empty;
                return;
            }

            bool canInteract = target.CanInteract(interactor);
            string prompt = target.GetPrompt(interactor);

            crosshair.color = canInteract ? activeColor : blockedColor;
            promptText.text = canInteract ? $"{keyLabel}: {prompt}" : prompt;
        }
    }
}
