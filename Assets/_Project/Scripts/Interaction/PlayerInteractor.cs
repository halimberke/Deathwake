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
        // Eşya yerleştirildi; konduğu poz ile (yankı aynı poza koyar).
        public event Action<Pose> Dropped;
        public event Action<IHoldable> HoldStarted;
        public event Action HoldEnded;

        public IInteractable CurrentTarget { get; private set; }

        // Açıkken girdi okunmaz ama tutulan düğme bırakılmaz (örnek: oyun duraklatıldı).
        // Bileşeni kapatmak (enabled = false) tutmayı bırakır; bu ise durumu olduğu gibi dondurur.
        public bool InputFrozen
        {
            get => inputFrozen;
            set
            {
                if (inputFrozen && !value)
                {
                    unfrozenFrame = Time.frameCount;
                }

                inputFrozen = value;
            }
        }

        private bool inputFrozen;
        // Donma kalktığı karede girdi okunmaz: paneli kapatan E aynı karede notu yeniden açmasın.
        private int unfrozenFrame = -1;

        // Aynı collider'a bakmaya devam ederken her karede GetComponent çağırmamak için.
        private Collider lastCollider;

        // Şu an basılı tutulan nesne. E bırakılınca ya da başka yere bakınca tutma biter.
        private IHoldable activeHold;

        private void OnEnable()
        {
            interactAction.action.Enable();
        }

        private void OnDisable()
        {
            // Ölünce bileşen kapanır; elimiz düğmeden çekilir.
            ReleaseHold();
            interactAction.action.Disable();
            SetTarget(null);
            lastCollider = null;
        }

        private void Update()
        {
            if (InputFrozen || Time.frameCount == unfrozenFrame)
            {
                return;
            }

            UpdateTarget();

            if (activeHold != null && activeHold.ReleasesOnInput &&
                (!interactAction.action.IsPressed() || CurrentTarget as IHoldable != activeHold))
            {
                ReleaseHold();
            }

            if (interactAction.action.WasPressedThisFrame())
            {
                TryInteract();
            }
        }

        private void ReleaseHold()
        {
            if (activeHold == null)
            {
                return;
            }

            activeHold.EndHold(this);
            activeHold = null;
            HoldEnded?.Invoke();
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

            if (target is IHoldable holdable)
            {
                if (holdable.CanHold(this))
                {
                    holdable.BeginHold(this);
                    activeHold = holdable;
                    HoldStarted?.Invoke(holdable);
                }
                return;
            }

            if (target != null && target.CanInteract(this))
            {
                target.Interact(this);
                Interacted?.Invoke(target);
                return;
            }

            if (CanDrop && hand.Drop(out Pose placed))
            {
                Dropped?.Invoke(placed);
            }
        }

        // Boşluğa ya da başka bir eşyaya bakarken E yerleştirme anlamına gelir.
        // Yerin uygun olup olmadığı HandSlot.TryGetPlacement ile sorulur (önizleme yeşil/kırmızı).
        // Kilitli kapı gibi başka bir hedefe bakarken bırakılmaz: yanlışlıkla eşya düşürmeyi önler.
        public bool CanDrop =>
            hand != null && hand.HeldItem != null &&
            (CurrentTarget == null || CurrentTarget is Pickup);
    }
}
