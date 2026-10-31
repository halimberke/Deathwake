using UnityEngine;

namespace Game.UI
{
    // Ana menünün arkasındaki canlı sahne: kendi kamerası yavaşça salınır.
    // Sahne ana menüyle yüklenir, oyun başlayınca atılır.
    public class MenuBackdrop : MonoBehaviour
    {
        [SerializeField] private Camera view;
        [Tooltip("Kameranın konum salınımı (m) ve bakış salınımı (derece).")]
        [SerializeField] private float swayDistance = 0.05f;
        [SerializeField] private float swayAngle = 0.5f;
        [SerializeField] private float swayPeriod = 11f;

        public static MenuBackdrop Current { get; private set; }

        public Camera View => view;

        private Vector3 restPosition;
        private Quaternion restRotation;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Current = null;
        }

        private void Awake()
        {
            restPosition = view.transform.position;
            restRotation = view.transform.rotation;
        }

        private void OnEnable()
        {
            Current = this;
        }

        private void OnDisable()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        private void Update()
        {
            float phase = Time.time * Mathf.PI * 2f / swayPeriod;
            view.transform.SetPositionAndRotation(
                restPosition + new Vector3(Mathf.Sin(phase), Mathf.Sin(phase * 0.7f) * 0.5f, 0f) * swayDistance,
                restRotation * Quaternion.Euler(Mathf.Sin(phase * 0.6f) * swayAngle, Mathf.Sin(phase * 0.8f) * swayAngle, 0f));
        }
    }
}
