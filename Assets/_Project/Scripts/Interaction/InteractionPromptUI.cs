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
        [SerializeField] private string dropPrompt = "Yerleştir";
        [SerializeField] private string noRoomPrompt = "Yer yok";
        [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color blockedColor = new Color(1f, 0.4f, 0.4f, 1f);

        private void Update()
        {
            // Yazı her karede okunur: kapı açılınca "Aç" yerine "Kapat" yazması gibi
            // hedef aynı kalsa da değişebilir. Tek bir arayüz çağrısı, maliyeti düşük.
            IInteractable target = interactor.CurrentTarget;

            bool canInteract = target != null && target.CanInteract(interactor);

            // Hedef yoksa ya da hedef bir eşyaysa (üstüne bırakmak için) bırakma yazısı gösterilir.
            if (!canInteract && interactor.CanDrop)
            {
                bool hasRoom = interactor.Hand.TryGetPlacement(out _, out bool valid) && valid;
                crosshair.color = hasRoom ? idleColor : blockedColor;
                promptText.text = hasRoom
                    ? $"{keyLabel}: {dropPrompt} ({interactor.Hand.HeldItem.DisplayName})"
                    : noRoomPrompt;
                return;
            }

            if (target == null)
            {
                crosshair.color = idleColor;
                promptText.text = string.Empty;
                return;
            }

            string prompt = target.GetPrompt(interactor);

            crosshair.color = canInteract ? activeColor : blockedColor;
            promptText.text = canInteract ? $"{keyLabel}: {prompt}" : prompt;
        }
    }
}
