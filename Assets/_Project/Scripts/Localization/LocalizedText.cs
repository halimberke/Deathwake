using TMPro;
using UnityEngine;

namespace Game.Localization
{
    // Sahnedeki sabit yazı. Inspector'daki Türkçe metni anahtar alır, dil değişince kendini yeniler.
    // Yazısı koddan atanan etiketlere eklenmez: kodun yazdığını eski metinle ezer.
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        private TMP_Text label;
        private string source;

        private void Awake()
        {
            label = GetComponent<TMP_Text>();
            source = label.text;
        }

        private void OnEnable()
        {
            Loc.Changed += Apply;
            Apply();
        }

        private void OnDisable()
        {
            Loc.Changed -= Apply;
        }

        private void Apply()
        {
            label.text = Loc.T(source);
        }
    }
}
