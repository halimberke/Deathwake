using UnityEngine;

namespace Game.Audio
{
    // Sahneden tetiklenen tek ses. Zaman çizelgesi ya da başka bir UnityEvent Play'i çağırır.
    public class SoundCue : MonoBehaviour
    {
        [SerializeField] private SoundSet sound;

        public void Play()
        {
            Sfx.Play(sound, transform.position);
        }
    }
}
