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

        private CharacterController controller;

        // Birden fazla neden girdiyi engelleyebilir. Biri bitince diğerini bozmasın diye ayrı tutulur.
        private bool controlEnabled = true;
        private bool fastForwardBlock;
        private bool holdLock; // Kelepçe gibi yerinde tutan bir şey tutuluyor: yürüme kapalı, bakış açık.

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            interactor.HoldStarted += HandleHoldStarted;
            interactor.HoldEnded += HandleHoldEnded;
        }

        private void OnDisable()
        {
            interactor.HoldStarted -= HandleHoldStarted;
            interactor.HoldEnded -= HandleHoldEnded;
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
            bool lookBlocked = !controlEnabled || fastForwardBlock;
            movement.SetInputBlocked(lookBlocked || holdLock);
            look.SetInputBlocked(lookBlocked);
        }
    }
}
