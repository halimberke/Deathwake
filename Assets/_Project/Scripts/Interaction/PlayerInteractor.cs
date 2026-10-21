using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Interaction
{
    // Kameranın ortasından ışın göndererek bakılan IInteractable'ı bulur ve E tuşunu işler.
    public class PlayerInteractor : MonoBehaviour, IInteractor
    {
        [Header("Input")]
        [SerializeField] private InputActionReference interactAction;

        [Header("Algılama")]
        [Tooltip("Işının çıktığı nokta. Genelde ana kamera.")]
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private float range = 2.5f;
        [Tooltip("Işının çarpabileceği katmanlar. Oyuncunun kendi katmanı hariç tutulmalı.")]
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("El")]
        [SerializeField] private HandSlot hand;

        public HandSlot Hand => hand;

        // Hedef değiştiğinde (yeni nesneye bakınca ya da boşluğa dönünce) tetiklenir. Null olabilir.
        public event Action<IInteractable> TargetChanged;

        // Kayıt sistemi bunları dinler. Etkileşim gerçekleştikten sonra tetiklenir.
        public event Action<IInteractable> Interacted;
        public event Action Dropped;

        public IInteractable CurrentTarget { get; private set; }

        // Aynı collider'a bakmaya devam ederken her karede GetComponent çağırmamak için.
        private Collider lastCollider;

        private void OnEnable()
        {
            interactAction.action.Enable();
        }

        private void OnDisable()
        {
            interactAction.action.Disable();
            SetTarget(null);
            lastCollider = null;
        }

        private void Update()
        {
            UpdateTarget();

            if (interactAction.action.WasPressedThisFrame())
            {
                TryInteract();
            }
        }

        private void UpdateTarget()
        {
            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

            // Trigger'lar yok sayılır. Basınç plakası gibi alanlar ışını engellemez.
            if (!Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                lastCollider = null;
                SetTarget(null);
                return;
            }

            if (hit.collider == lastCollider)
            {
                return;
            }

            lastCollider = hit.collider;
            SetTarget(hit.collider.GetComponentInParent<IInteractable>());
        }

        private void SetTarget(IInteractable target)
        {
            if (target == CurrentTarget)
            {
                return;
            }

            CurrentTarget = target;
            TargetChanged?.Invoke(target);
        }

        // Hedef varsa onunla etkileşir. Hedef yoksa ve el doluysa eşyayı bırakır.
        // Kilitli bir hedefe bakarken bırakılmaz: yanlışlıkla eşya düşürmeyi önler.
        private void TryInteract()
        {
            IInteractable target = CurrentTarget;

            if (target != null)
            {
                if (target.CanInteract(this))
                {
                    target.Interact(this);
                    Interacted?.Invoke(target);
                }
                return;
            }

            if (CanDrop)
            {
                hand.Drop();
                Dropped?.Invoke();
            }
        }

        public bool CanDrop => CurrentTarget == null && hand != null && hand.HeldItem != null;
    }
}
