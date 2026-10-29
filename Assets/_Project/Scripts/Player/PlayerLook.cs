using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    // Fare ile bakış. Sağ-sol dönüş gövdeyi, yukarı-aşağı bakış kamerayı döndürür.
    public class PlayerLook : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference lookAction;

        [Header("Kamera")]
        [Tooltip("Kamerayı taşıyan nesne. Yukarı-aşağı dönüş buna uygulanır.")]
        [SerializeField] private Transform cameraPivot;

        [Header("Ayarlar")]
        [Tooltip("Fare hareketinin her pikseli için derece.")]
        [SerializeField] private float sensitivity = 0.1f;
        [SerializeField] private float minPitch = -85f;
        [SerializeField] private float maxPitch = 85f;
        [SerializeField] private bool lockCursorOnEnable = true;

        private float pitch;
        private bool inputBlocked;

        // Senaryo animasyonları (uyanma) kamerayı geçici olarak buradan oynatır.
        public Transform CameraPivot => cameraPivot;

        // Fare yok sayılır, imleç kilitli kalır. Örnek: zaman hızlandırılırken.
        public void SetInputBlocked(bool blocked)
        {
            inputBlocked = blocked;
        }

        // Işınlandıktan sonra bakış ileriye döner.
        public void ResetPitch()
        {
            pitch = 0f;
            cameraPivot.localRotation = Quaternion.identity;
        }

        private void OnEnable()
        {
            lookAction.action.Enable();

            if (lockCursorOnEnable)
            {
                SetCursorLocked(true);
            }
        }

        private void OnDisable()
        {
            lookAction.action.Disable();
            SetCursorLocked(false);
        }

        private void Update()
        {
            if (inputBlocked)
            {
                return;
            }

            // Fare deltası zaten kare başına gelir. Time.deltaTime ile çarpılmaz.
            Vector2 delta = lookAction.action.ReadValue<Vector2>() * sensitivity;

            transform.Rotate(Vector3.up, delta.x, Space.Self);

            pitch = Mathf.Clamp(pitch - delta.y, minPitch, maxPitch);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
