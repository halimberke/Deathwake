using Game.Chamber;
using UnityEngine;

namespace Game.Echo
{
    // Her deneme başında, odanın kayıtlı her denemesi için bir yankı oluşturur.
    public class EchoSpawner : MonoBehaviour
    {
        [SerializeField] private ChamberController chamber;
        [SerializeField] private GhostActor ghostPrefab;

        private void OnEnable()
        {
            chamber.StateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            chamber.StateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(ChamberState previous, ChamberState next)
        {
            if (next != ChamberState.Running)
            {
                return;
            }

            int index = 0;
            foreach (GhostRecording recording in chamber.Progress.Recordings)
            {
                GhostActor ghost = Instantiate(ghostPrefab, transform);
                ghost.name = $"Echo_{index++}";
                ghost.Init(recording, chamber.Clock);
            }
        }
    }
}
