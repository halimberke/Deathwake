using TMPro;
using UnityEngine;

namespace Game.Chamber
{
    // Ekran köşesinde deneme sayısı, kalan süre ve durum yazısı. Prototip için.
    public class ChamberStatusUI : MonoBehaviour
    {
        [SerializeField] private ChamberController chamber;
        [SerializeField] private TMP_Text label;

        private void Update()
        {
            int attempt = chamber.Progress.AttemptCount;
            float remaining = chamber.Clock.Remaining;

            label.text = chamber.State switch
            {
                ChamberState.Preparing => $"Deneme {attempt + 1}",
                ChamberState.Running => $"Deneme {attempt}   {remaining:0.0}",
                ChamberState.Dead => $"Öldün: {chamber.LastDeathReason}",
                ChamberState.Solved => "Oda çözüldü",
                _ => string.Empty
            };
        }
    }
}
