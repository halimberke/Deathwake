using TMPro;
using UnityEngine;

namespace Game.Chamber
{
    // Ekran köşesinde deneme sayısı, kalan süre ve durum yazısı. Prototip için.
    // Aktif odayı ActiveChamber'dan okur; oda yoksa (geçiş kabininde) boş kalır.
    public class ChamberStatusUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [Tooltip("Boşsa hızlandırma bilgisi gösterilmez.")]
        [SerializeField] private ChamberTimeControl timeControl;
        [Tooltip("{0} = atanmış tuş.")]
        [SerializeField] private string fastForwardHint = "[{0}] hızlandır";

        private void Update()
        {
            ChamberController chamber = ActiveChamber.Current;
            if (chamber == null)
            {
                label.text = string.Empty;
                return;
            }

            int attempt = chamber.Progress.AttemptCount;
            string remaining = chamber.Definition.HasTimeLimit ? $"   {chamber.Clock.Remaining:0.0}" : string.Empty;

            label.text = chamber.State switch
            {
                ChamberState.Preparing => $"Deneme {attempt + 1}",
                ChamberState.Running => $"Deneme {attempt}{remaining}{TimeSuffix()}",
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
                : "   " + string.Format(fastForwardHint, timeControl.KeyLabel);
        }
    }
}
