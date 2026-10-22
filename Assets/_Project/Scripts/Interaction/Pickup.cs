using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Interaction
{
    // Alınıp taşınabilen eşya. Elde tutulurken fiziği kapanır, bırakılınca fizikle düşer.
    [RequireComponent(typeof(Rigidbody))]
    public class Pickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemDefinition definition;
        [SerializeField] private string prompt = "Al";
        [Tooltip("Elde tutulurken boyut çarpanı. Büyük eşyalar ekranı kapatmasın diye küçültülür.")]
        [SerializeField] private float heldScale = 1f;

        private Rigidbody body;
        private Vector3 originalScale;
        private Collider[] colliders;
        private RigidbodyInterpolation originalInterpolation;

        // Eşyanın ait olduğu oda. Ele alınınca oyuncunun sahnesine (Core) geçer;
        // bırakılınca buraya döner ki oda yeniden yüklenince eşya da gitsin.
        private Transform homeParent;
        private Scene homeScene;

        public ItemDefinition Definition => definition;

        // Şu an bir aktörün elinde mi? Yankı, oyuncunun elindeki eşyayı alamaz (paradoks).
        public bool IsHeld { get; private set; }

        // Bir yuvaya yerleştirildi mi? Yerleşen eşya artık alınamaz.
        public bool IsPlaced { get; private set; }

        // Tanım atanmadıysa nesne adı kullanılır. Test nesneleri için kolaylık.
        public string DisplayName => definition != null ? definition.DisplayName : name;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>();
            originalInterpolation = body.interpolation;
            originalScale = transform.localScale;
            homeParent = transform.parent;
            homeScene = gameObject.scene;
        }

        public string GetPrompt(IInteractor interactor)
        {
            return $"{prompt} ({DisplayName})";
        }

        public bool CanInteract(IInteractor interactor)
        {
            return interactor.Hand != null && !IsHeld && !IsPlaced;
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
            transform.localScale = originalScale * heldScale;
        }

        // HandSlot ya da geçiş kutusu tarafından çağrılır.
        public void Release(Vector3 position, Quaternion rotation)
        {
            ReturnHome();
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = originalScale;

            IsHeld = false;
            SetCollidersEnabled(true);
            body.isKinematic = false;
            body.interpolation = originalInterpolation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        // Yuvaya yerleştirir: fizik kapalı, sabit, artık alınamaz ama üstüne basılabilir.
        public void Place(Transform point)
        {
            ReturnHome();
            transform.SetPositionAndRotation(point.position, point.rotation);
            transform.localScale = originalScale;

            IsHeld = false;
            IsPlaced = true;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None;
            SetCollidersEnabled(true);
            Physics.SyncTransforms();
        }

        // SetParent(null) nesneyi bulunduğu sahnede bırakır; elde tutulurken bu sahne Core'dur.
        // Bu yüzden eşya ilk ebeveynine, o yoksa kendi sahnesine açıkça geri taşınır.
        private void ReturnHome()
        {
            if (homeParent != null)
            {
                transform.SetParent(homeParent, true);
                return;
            }

            transform.SetParent(null, true);
            if (homeScene.IsValid() && homeScene.isLoaded && gameObject.scene != homeScene)
            {
                SceneManager.MoveGameObjectToScene(gameObject, homeScene);
            }
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
