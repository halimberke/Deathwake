using System.Collections;
using Game.Localization;
using UnityEngine;

namespace Game.Interaction
{
    // Menteşe etrafında dönen kapı. Kilitli olabilir; anahtarla ya da bir olayla (düğme, plaka) açılır.
    public class Door : MonoBehaviour, IInteractable
    {
        [Header("Hareket")]
        [Tooltip("Dönen parça. Pivotu menteşe hizasında olmalı. Boşsa bu nesne döner.")]
        [SerializeField] private Transform hinge;
        [Tooltip("Açık hâldeki Y açısı (kapalıya göre). Ters yöne açmak için negatif ver.")]
        [SerializeField] private float openAngle = 90f;
        [Tooltip("Derece/saniye.")]
        [SerializeField] private float rotateSpeed = 180f;
        [SerializeField] private bool startOpen;

        [Header("Kilit")]
        [SerializeField] private bool isLocked;
        [Tooltip("Kilidi açan eşya. Boşsa kapı elle açılamaz, sadece olayla açılır.")]
        [SerializeField] private ItemDefinition requiredItem;

        [Header("Yazılar")]
        [SerializeField] private string openPrompt = "Aç";
        [SerializeField] private string closePrompt = "Kapat";
        [SerializeField] private string unlockPrompt = "Kilidi aç";
        [SerializeField] private string lockedPrompt = "Kilitli";

        private Quaternion closedRotation;
        private bool isOpen;
        private Coroutine moveRoutine;

        public bool IsOpen => isOpen;
        public bool IsLocked => isLocked;

        private void Awake()
        {
            if (hinge == null)
            {
                hinge = transform;
            }

            closedRotation = hinge.localRotation;
            isOpen = startOpen;
            hinge.localRotation = TargetRotation();
        }

        public string GetPrompt(IInteractor interactor)
        {
            if (isLocked)
            {
                if (CanUnlockWithHand(interactor))
                {
                    return $"{Loc.T(unlockPrompt)} ({requiredItem.DisplayName})";
                }

                return requiredItem != null
                    ? Loc.Format("{0}: {1} gerekli", Loc.T(lockedPrompt), requiredItem.DisplayName)
                    : Loc.T(lockedPrompt);
            }

            return Loc.T(isOpen ? closePrompt : openPrompt);
        }

        public bool CanInteract(IInteractor interactor)
        {
            return !isLocked || CanUnlockWithHand(interactor);
        }

        public void Interact(IInteractor interactor)
        {
            if (isLocked)
            {
                Unlock();
                Open();
                return;
            }

            Toggle();
        }

        // Aşağıdakiler UnityEvent'ten de çağrılabilir (düğme, basınç plakası).
        public void Open() => SetOpen(true);
        public void Close() => SetOpen(false);
        public void Toggle() => SetOpen(!isOpen);
        public void Unlock() => isLocked = false;
        public void Lock() => isLocked = true;

        private bool CanUnlockWithHand(IInteractor interactor)
        {
            return requiredItem != null && interactor.Hand != null && interactor.Hand.IsHolding(requiredItem);
        }

        private void SetOpen(bool open)
        {
            if (open == isOpen)
            {
                return;
            }

            isOpen = open;

            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
            }
            moveRoutine = StartCoroutine(RotateTo(TargetRotation()));
        }

        private Quaternion TargetRotation()
        {
            return isOpen ? closedRotation * Quaternion.Euler(0f, openAngle, 0f) : closedRotation;
        }

        // Coroutine: her karede biraz döndürür, "yield return null" ile bir sonraki kareyi bekler.
        private IEnumerator RotateTo(Quaternion target)
        {
            while (Quaternion.Angle(hinge.localRotation, target) > 0.1f)
            {
                hinge.localRotation = Quaternion.RotateTowards(hinge.localRotation, target, rotateSpeed * Time.deltaTime);
                yield return null;
            }

            hinge.localRotation = target;
            moveRoutine = null;
        }
    }
}
