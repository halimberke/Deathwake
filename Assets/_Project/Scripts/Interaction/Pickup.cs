using UnityEngine;

namespace Game.Interaction
{
    // Alınıp taşınabilen eşya. Elde tutulurken fiziği kapanır, bırakılınca fizikle düşer.
    [RequireComponent(typeof(Rigidbody))]
    public class Pickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemDefinition definition;
        [SerializeField] private string prompt = "Al";

        private Rigidbody body;
        private Collider[] colliders;
        private RigidbodyInterpolation originalInterpolation;

        public ItemDefinition Definition => definition;

        // Şu an bir aktörün elinde mi? Yankı, oyuncunun elindeki eşyayı alamaz (paradoks).
        public bool IsHeld { get; private set; }

        // Tanım atanmadıysa nesne adı kullanılır. Test nesneleri için kolaylık.
        public string DisplayName => definition != null ? definition.DisplayName : name;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>();
            originalInterpolation = body.interpolation;
        }

        public string GetPrompt(IInteractor interactor)
        {
            return $"{prompt} ({DisplayName})";
        }

        public bool CanInteract(IInteractor interactor)
        {
            return interactor.Hand != null && !IsHeld;
        }

        public void Interact(IInteractor interactor)
        {
            interactor.Hand.Hold(this);
        }

        // HandSlot tarafından çağrılır.
        public void AttachTo(Transform holdPoint)
        {
            IsHeld = true;
            body.isKinematic = true;
            // Interpolation açık kalırsa Unity nesneyi eski fizik konumundan yavaşça çeker
            // ve eşya kameranın gerisinde kalır. Elde tutulurken kapatılır.
            body.interpolation = RigidbodyInterpolation.None;
            SetCollidersEnabled(false);

            transform.SetParent(holdPoint, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        // HandSlot tarafından çağrılır.
        public void Release(Vector3 position, Quaternion rotation)
        {
            transform.SetParent(null, true);
            transform.SetPositionAndRotation(position, rotation);

            IsHeld = false;
            SetCollidersEnabled(true);
            body.isKinematic = false;
            body.interpolation = originalInterpolation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        private void SetCollidersEnabled(bool value)
        {
            foreach (Collider c in colliders)
            {
                c.enabled = value;
            }
        }
    }
}
