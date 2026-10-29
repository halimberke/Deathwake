using System.Collections;
using Game.Interaction;
using UnityEngine;

namespace Game.Player
{
    // Oyuncunun parçalarını tek yerden yönetir: ışınlanma, kontrol, el.
    // Oyun akışı (ölüm, oda geçişi) ve hızlandırma oyuncuya bunun üzerinden dokunur.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerRig : MonoBehaviour
    {
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerLook look;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private HandSlot hand;

        [Header("Uyanma animasyonu")]
        [Tooltip("Başlangıçta kamera normal göz hizasından bu kadar aşağıda (yatakta yatar gibi).")]
        [SerializeField] private float wakeDrop = 0.9f;
        [Tooltip("Başlangıç bakışı: negatif yukarı (tavana) bakar.")]
        [SerializeField] private float wakeStartPitch = -80f;
        [SerializeField] private float wakeStartRoll = 20f;

        private CharacterController controller;

        // Birden fazla neden girdiyi engelleyebilir. Biri bitince diğerini bozmasın diye ayrı tutulur.
        private bool controlEnabled = true;
        private bool fastForwardBlock;
        private bool holdLock; // Kelepçe gibi yerinde tutan bir şey tutuluyor: yürüme kapalı, bakış açık.
        private bool paused;
        private bool reading; // Not ya da defter açık: oda saati akar ama oyuncu donar, fare serbest.
        private bool scripted; // Senaryo oyuncuyu tutuyor (uyanma, kaçırılma): her şey donar, fare kilitli.

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            interactor.HoldStarted += HandleHoldStarted;
            interactor.HoldEnded += HandleHoldEnded;
            hand.HeldItemChanged += HandleHeldItemChanged;
        }

        private void OnDisable()
        {
            interactor.HoldStarted -= HandleHoldStarted;
            interactor.HoldEnded -= HandleHoldEnded;
            hand.HeldItemChanged -= HandleHeldItemChanged;
        }

        // Ağır eşya taşırken yavaşla.
        private void HandleHeldItemChanged(Pickup item)
        {
            float multiplier = item != null && item.Definition != null ? item.Definition.CarrySpeedMultiplier : 1f;
            movement.SetSpeedMultiplier(multiplier);
        }

        private void HandleHoldStarted(IHoldable holdable)
        {
            holdLock = holdable.LocksMovement;
            ApplyInputBlock();
        }

        private void HandleHoldEnded()
        {
            holdLock = false;
            ApplyInputBlock();
        }

        // Ölümde kapanır, yeniden doğunca açılır.
        public void SetControlEnabled(bool enabled)
        {
            controlEnabled = enabled;
            // Etkileşim kapanınca tutulan düğme bırakılır (PlayerInteractor.OnDisable).
            interactor.enabled = enabled;
            ApplyInputBlock();
        }

        // Zaman hızlandırılırken hareket ve bakış durur, tutulan düğme bırakılmaz.
        public void SetFastForwardBlock(bool blocked)
        {
            fastForwardBlock = blocked;
            ApplyInputBlock();
        }

        // Duraklatma: hareket, bakış ve etkileşim donar, fare serbest kalır. Tutulan şeyler bırakılmaz.
        public void SetPaused(bool value)
        {
            paused = value;
            ApplyUiState();
        }

        // Not okuma ve defter: hareket, bakış ve etkileşim donar, fare serbest kalır. Zaman akmaya devam eder.
        public void SetReading(bool value)
        {
            reading = value;
            ApplyUiState();
        }

        // Hikâye sahnelerinde senaryo oyuncuyu bir süre yerinde tutar.
        public void SetScriptedBlock(bool value)
        {
            scripted = value;
            ApplyUiState();
        }

        // Yatakta uyanma: kamera alçaktan, tavana bakarak başlar; doğrulup ileri bakar.
        // Bitince bakış ileri döner. Kontrol kendiliğinden açılmaz; senaryo SetScriptedBlock(false) der.
        public void PlayWakeUp(float duration)
        {
            StartCoroutine(WakeUpRoutine(duration));
        }

        private IEnumerator WakeUpRoutine(float duration)
        {
            SetScriptedBlock(true);
            Transform pivot = look.CameraPivot;
            Vector3 basePosition = pivot.localPosition;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                pivot.localPosition = basePosition + Vector3.down * Mathf.Lerp(wakeDrop, 0f, k);
                pivot.localRotation = Quaternion.Euler(Mathf.Lerp(wakeStartPitch, 0f, k), 0f, Mathf.Lerp(wakeStartRoll, 0f, k));
                yield return null;
            }

            pivot.localPosition = basePosition;
            look.ResetPitch();
        }

        private void ApplyUiState()
        {
            bool uiOpen = paused || reading;
            interactor.InputFrozen = uiOpen || scripted;
            look.SetCursorLocked(!uiOpen);
            ApplyInputBlock();
        }

        public void TeleportTo(Transform point)
        {
            // CharacterController açıkken transform'u doğrudan değiştirmek güvenilir değildir.
            controller.enabled = false;
            transform.SetPositionAndRotation(point.position, Quaternion.Euler(0f, point.eulerAngles.y, 0f));
            controller.enabled = true;

            movement.ResetVelocity();
            look.ResetPitch();
        }

        // Eşyalar odalar ve denemeler arasında taşınmaz: eldeki yok edilir.
        public void ClearHand()
        {
            Pickup item = hand.Take();
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        private void ApplyInputBlock()
        {
            bool lookBlocked = !controlEnabled || fastForwardBlock || paused || reading || scripted;
            movement.SetInputBlocked(lookBlocked || holdLock);
            look.SetInputBlocked(lookBlocked);
        }
    }
}
