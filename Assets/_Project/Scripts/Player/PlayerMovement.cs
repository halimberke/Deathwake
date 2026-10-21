using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    // Yürüme, koşma, zıplama ve yerçekimi. Bakış yönü PlayerLook'ta.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference sprintAction;
        [SerializeField] private InputActionReference jumpAction;

        [Header("Hareket")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float sprintSpeed = 6.5f;
        [Tooltip("Hedef hıza ulaşma hızı (m/s^2). Büyük değer = daha keskin hareket.")]
        [SerializeField] private float acceleration = 30f;

        [Header("Zıplama ve yerçekimi")]
        [SerializeField] private float jumpHeight = 1f;
        [SerializeField] private float gravity = -20f;
        [Tooltip("Yerdeyken aşağı uygulanan küçük hız. Rampalarda zemine yapışık kalmayı sağlar.")]
        [SerializeField] private float groundedStickVelocity = -2f;

        private CharacterController controller;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private bool inputBlocked;

        // Girdi yok sayılır ama yerçekimi ve yavaşlama sürer. Örnek: zaman hızlandırılırken.
        public void SetInputBlocked(bool blocked)
        {
            inputBlocked = blocked;
        }

        // Işınlandıktan sonra eski hız taşınmasın.
        public void ResetVelocity()
        {
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            moveAction.action.Enable();
            sprintAction.action.Enable();
            jumpAction.action.Enable();
        }

        private void OnDisable()
        {
            moveAction.action.Disable();
            sprintAction.action.Disable();
            jumpAction.action.Disable();
        }

        private void Update()
        {
            UpdateHorizontal();
            UpdateVertical();

            Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }

        private void UpdateHorizontal()
        {
            Vector2 input = inputBlocked ? Vector2.zero : moveAction.action.ReadValue<Vector2>();

            // Girdiyi karakterin baktığı yöne göre dünya uzayına çevir.
            Vector3 wishDirection = transform.right * input.x + transform.forward * input.y;
            if (wishDirection.sqrMagnitude > 1f)
            {
                wishDirection.Normalize();
            }

            float targetSpeed = sprintAction.action.IsPressed() ? sprintSpeed : walkSpeed;
            Vector3 targetVelocity = wishDirection * targetSpeed;

            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);
        }

        private void UpdateVertical()
        {
            bool isGrounded = controller.isGrounded;

            if (isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedStickVelocity;
            }

            if (isGrounded && !inputBlocked && jumpAction.action.WasPressedThisFrame())
            {
                // v = sqrt(2 * g * h) formülü. Gravity negatif olduğu için -2 ile çarpılır.
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;
        }
    }
}
