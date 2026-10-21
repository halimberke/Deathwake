using UnityEngine;

namespace Game.Chamber
{
    // Odanın çıkışı. Oyuncu bu alana girince oda çözülür.
    // Trigger collider gerekir. Yankıların collider'ı olmadığı için onlar tetiklemez.
    [RequireComponent(typeof(Collider))]
    public class ChamberExit : MonoBehaviour
    {
        [SerializeField] private ChamberController chamber;
        [Tooltip("Sadece bu etiketteki nesne tetikler.")]
        [SerializeField] private string playerTag = "Player";

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(playerTag))
            {
                chamber.Solve();
            }
        }
    }
}
