using UnityEngine;

namespace Game.Audio
{
    // Oyundaki kısa seslerin tek girişi: Sfx.Play(ses, konum). Kapı, eşya ve taret bunu çağırır.
    // Çalmayı Core'daki SfxPlayer yapar; oda sahnesi atılsa da sesin kuyruğu kesilmez.
    public static class Sfx
    {
        public static SfxPlayer Player { get; set; }

        public static void Play(SoundSet sound, Vector3 position, float gain = 1f)
        {
            if (sound != null && Player != null)
            {
                Player.Play(sound, position, gain);
            }
        }
    }
}
