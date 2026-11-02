using System.Collections.Generic;
using Game.Audio;
using Game.Localization;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Interaction
{
    // Basılı tutulduğu sürece aktif olan düğme. En az bir aktör tutuyorsa basılıdır.
    public class HoldButton : MonoBehaviour, IInteractable, IHoldable
    {
        [SerializeField] private string prompt = "Basılı tut";
        [SerializeField] private string heldPrompt = "Tutuluyor";

        [Header("Tutma türü")]
        [Tooltip("Kapalıysa E bırakılınca tutma bitmez; sadece tutan ölünce biter (kelepçe gibi).")]
        [SerializeField] private bool releasesOnInput = true;
        [Tooltip("Açıksa tutan oyuncu yürüyemez.")]
        [SerializeField] private bool locksMovement;

        [Header("Görsel")]
        [Tooltip("Rengi değişecek parça. Boşsa görsel geri bildirim yok.")]
        [SerializeField] private Renderer indicator;
        [SerializeField] private Color idleColor = new Color(0.6f, 0.15f, 0.15f);
        [SerializeField] private Color pressedColor = new Color(0.2f, 0.9f, 0.3f);

        [Header("Ses")]
        [SerializeField] private SoundSet pressSound;
        [SerializeField] private SoundSet releaseSound;

        [Header("Olaylar")]
        [SerializeField] private UnityEvent onPressed;
        [SerializeField] private UnityEvent onReleased;

        private readonly HashSet<IInteractor> holders = new();
        private MaterialPropertyBlock propertyBlock;

        public bool IsPressed => holders.Count > 0;
        public bool ReleasesOnInput => releasesOnInput;
        public bool LocksMovement => locksMovement;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            UpdateVisual();
        }

        // IInteractable: ekrandaki yazı için. Asıl iş IHoldable üzerinden yürür.
        public string GetPrompt(IInteractor interactor)
        {
            return Loc.T(holders.Contains(interactor) ? heldPrompt : prompt);
        }

        public bool CanInteract(IInteractor interactor)
        {
            return CanHold(interactor);
        }

        public void Interact(IInteractor interactor)
        {
            // Basılı tutulan nesnede tek basış bir şey yapmaz.
        }

        public bool CanHold(IInteractor interactor)
        {
            return !holders.Contains(interactor);
        }

        public void BeginHold(IInteractor interactor)
        {
            bool wasPressed = IsPressed;
            holders.Add(interactor);

            if (!wasPressed)
            {
                UpdateVisual();
                Sfx.Play(pressSound, transform.position);
                onPressed.Invoke();
            }
        }

        public void EndHold(IInteractor interactor)
        {
            if (!holders.Remove(interactor) || IsPressed)
            {
                return;
            }

            UpdateVisual();
            Sfx.Play(releaseSound, transform.position);
            onReleased.Invoke();
        }

        // MaterialPropertyBlock: malzemenin kopyasını oluşturmadan sadece bu nesnenin rengini değiştirir.
        private void UpdateVisual()
        {
            if (indicator == null)
            {
                return;
            }

            indicator.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", IsPressed ? pressedColor : idleColor);
            indicator.SetPropertyBlock(propertyBlock);
        }
    }
}
