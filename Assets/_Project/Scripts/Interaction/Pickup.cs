using Game.Localization;
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
        [Tooltip("Bu hızın üstünde hareket eden eşya alınamaz (havada uçan eşyayı yakalamak gibi).")]
        [SerializeField] private float maxPickupSpeed = 0.5f;
        [Tooltip("Fizik motorunun iç içe geçmiş nesneleri ayırırken verebileceği en yüksek hız. Fırlamayı önler.")]
        [SerializeField] private float maxDepenetrationSpeed = 1f;
        [Tooltip("Açıksa eşya devrilmez (sadece kendi ekseninde dönebilir). Kasalar rampaya dönüşmesin diye.")]
        [SerializeField] private bool keepUpright;

        private Rigidbody body;
        private Vector3 originalScale;
        private Collider[] colliders;
        private RigidbodyInterpolation originalInterpolation;

        // Eşyanın ait olduğu oda. Ele alınınca oyuncunun sahnesine (Core) geçer;
        // bırakılınca buraya döner ki oda yeniden yüklenince eşya da gitsin.
        private Transform homeParent;
        private Scene homeScene;

        public ItemDefinition Definition => definition;

        // Durmuş mu? Hareket hâlindeki eşya alınmaz.
        private bool IsSettled => body.isKinematic || body.linearVelocity.sqrMagnitude <= maxPickupSpeed * maxPickupSpeed;

        // Normal boyuttaki yarı ölçüler. Bırakırken yüzeyin içine gömülmemek için kullanılır.
        public Vector3 Extents { get; private set; }

        // Normal (elde küçülmemiş) boyut. Yerleştirme önizlemesi bunu kullanır.
        public Vector3 OriginalScale => originalScale;

        // Şu an bir aktörün elinde mi? Yankı, oyuncunun elindeki eşyayı alamaz (paradoks).
        public bool IsHeld { get; private set; }

        // Tanım atanmadıysa nesne adı kullanılır. Test nesneleri için kolaylık.
        public string DisplayName => definition != null ? definition.DisplayName : name;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>();
            originalInterpolation = body.interpolation;
            body.maxDepenetrationVelocity = maxDepenetrationSpeed;
            if (keepUpright)
            {
                body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
            originalScale = transform.localScale;
            Extents = transform.lossyScale * 0.5f;
            homeParent = transform.parent;
            homeScene = gameObject.scene;
        }

        public string GetPrompt(IInteractor interactor)
        {
            return $"{Loc.T(prompt)} ({DisplayName})";
        }

        public bool CanInteract(IInteractor interactor)
        {
            // El doluyken alınmaz: bakılan eşyanın üstüne bırakılır (PlayerInteractor.CanDrop).
            return interactor.Hand != null && interactor.Hand.HeldItem == null && !IsHeld
                   && IsSettled;
        }

        public void Interact(IInteractor interactor)
        {
            interactor.Hand.Hold(this);
        }

        // HandSlot tarafından çağrılır.
        // fullSize: eşya gerçek boyutunda kalır (yankı). Oyuncuda heldScale ekranı kapatmamak içindir.
        public void AttachTo(Transform holdPoint, bool fullSize = false)
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
            transform.localScale = fullSize ? originalScale : originalScale * heldScale;
        }

        // HandSlot ya da geçiş kutusu tarafından çağrılır.
        public void Release(Vector3 position, Quaternion rotation)
        {
            MoveTo(position, rotation);

            IsHeld = false;
            SetCollidersEnabled(true);
            body.isKinematic = false;
            body.interpolation = originalInterpolation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            // Kinematikten çıkan gövde "uyuyor" kalabilir ve havada asılı durur; yerçekimi için uyandırılır.
            body.WakeUp();
        }

        // Eşyayı elden alıp hedef poza taşır. Collider'lar açılmadan ÖNCE çağrılmalı.
        // Transform'u değiştirmek fizik gövdesini hemen taşımaz: gövde bir fizik adımı boyunca
        // eski yerinde (elde, oyuncunun göğsünde) tam boyutuyla kalır ve oyuncuyu yukarı iter.
        // Bu yüzden gövde de açıkça taşınır ve fizik dünyası hemen güncellenir.
        private void MoveTo(Vector3 position, Quaternion rotation)
        {
            ReturnHome();
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = originalScale;
            body.position = position;
            body.rotation = rotation;
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
