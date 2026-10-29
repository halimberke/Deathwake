using UnityEngine;
using UnityEngine.Events;

namespace Game.Story
{
    // Senaryo için alan: oyuncu içinde mi? Collider'ı trigger olmalı.
    [RequireComponent(typeof(Collider))]
    public class ScriptZone : MonoBehaviour
    {
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private UnityEvent onPlayerEnter;

        public bool PlayerInside { get; private set; }

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(playerTag))
            {
                PlayerInside = true;
                onPlayerEnter.Invoke();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(playerTag))
            {
                PlayerInside = false;
            }
        }
    }
}
