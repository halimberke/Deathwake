using TMPro;
using UnityEngine;

namespace Game.Chamber
{
    // Ekran köşesinde deneme sayısı, kalan süre ve durum yazısı. Prototip için.
    public class ChamberStatusUI : MonoBehaviour
    {
        [SerializeField] private ChamberController chamber;
        [SerializeField] private TMP_Text label;
        [Tooltip("Boşsa hızlandırma bilgisi gösterilmez.")]
        [SerializeField] private ChamberTimeControl timeControl;
        [SerializeField] private string fastForwardHint = "[F] hızlandır";

        private void Update()
        {
            int attempt = chamber.Progress.AttemptCount;
            float remaining = chamber.Clock.Remaining;

            label.text = chamber.State switch
            {
                ChamberState.Preparing => $"Deneme {attempt + 1}",
                ChamberState.Running => $"Deneme {attempt}   {remaining:0.0}{TimeSuffix()}",
                ChamberState.Dead => $"Öldün: {chamber.LastDeathReason}",
                ChamberState.Solved => "Oda çözüldü",
                _ => string.Empty
            };
        }

        private string TimeSuffix()
        {
            if (timeControl == null || !timeControl.IsAvailable)
            {
                return string.Empty;
            }

            return timeControl.IsFastForwarding
                ? $"   >> ×{timeControl.Speed:0}"
                : $"   {fastForwardHint}";
        }
    }
}
