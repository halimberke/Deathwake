using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Game.Chamber
{
    // Tuş basılı tutulurken zamanı hızlandırır. Beklemeyi kısaltmak ve yankıları izlemek için.
    // Oda saati, gaz, yankılar ve fizik aynı Time.timeScale'e bağlı olduğundan hepsi birlikte hızlanır;
    // yankılar senkrondan çıkmaz.
    public class ChamberTimeControl : MonoBehaviour
    {
        [SerializeField] private ChamberController chamber;
        [SerializeField] private InputActionReference fastForwardAction;
        [SerializeField] private float speed = 4f;
        [Tooltip("Açıksa hızlandırma bu odada ilk ölümden sonra kullanılabilir.")]
        [SerializeField] private bool requireFirstDeath = true;

        [Tooltip("Hızlandırma başlayınca true, bitince false ile çağrılır. Örnek: oyuncu hareketini engellemek.")]
        [SerializeField] private UnityEvent<bool> onFastForwardChanged;

        public bool IsFastForwarding { get; private set; }
        public float Speed => speed;

        public bool IsAvailable =>
            chamber.State == ChamberState.Running &&
            (!requireFirstDeath || chamber.Progress.AttemptCount > 1);

        private void OnEnable()
        {
            fastForwardAction.action.Enable();
        }

        private void OnDisable()
        {
            fastForwardAction.action.Disable();
            Apply(false);
        }

        private void Update()
        {
            Apply(IsAvailable && fastForwardAction.action.IsPressed());
        }

        private void Apply(bool fastForward)
        {
            // Time.timeScale tüm oyunu etkiler ve sahne değişse de kalır; bu yüzden her durumda geri alınır.
            Time.timeScale = fastForward ? speed : 1f;

            if (fastForward == IsFastForwarding)
            {
                return;
            }

            IsFastForwarding = fastForward;
            onFastForwardChanged.Invoke(fastForward);
        }
    }
}
