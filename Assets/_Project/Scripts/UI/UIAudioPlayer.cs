using Game.Audio;
using UnityEngine;

namespace Game.UI
{
    // Core'da yaşar, arayüz seslerini çalar. Duraklatmada da duyulsun diye kaynakları
    // "Ignore Listener Pause" ayarlıdır (ProceduralSound).
    public class UIAudioPlayer : MonoBehaviour
    {
        [SerializeField] private ProceduralSound hover;
        [SerializeField] private ProceduralSound click;

        private void OnEnable()
        {
            UIAudio.Hovered += hover.Play;
            UIAudio.Clicked += click.Play;
        }

        private void OnDisable()
        {
            UIAudio.Hovered -= hover.Play;
            UIAudio.Clicked -= click.Play;
        }
    }
}
