using UnityEngine;

namespace Game.Audio
{
    // Core'da yaşar. Hazır bekleyen ses kaynaklarından oluşan bir havuz: her ses için yeni nesne açılmaz.
    // Havuz doluysa en eski ses kesilir.
    public class SfxPlayer : MonoBehaviour
    {
        [Tooltip("Aynı anda çalabilecek kısa ses sayısı.")]
        [SerializeField] private int voices = 24;
        [Tooltip("Geliştirme: çalan her sesi konsola yazar.")]
        [SerializeField] private bool logPlays;

        private AudioSource[] sources;
        private float[] startTimes;

        private void Awake()
        {
            sources = new AudioSource[voices];
            startTimes = new float[voices];
            for (int i = 0; i < voices; i++)
            {
                var voice = new GameObject($"Voice_{i}");
                voice.transform.SetParent(transform, false);
                AudioSource source = voice.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.dopplerLevel = 0f;
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                sources[i] = source;
            }

            Sfx.Player = this;
        }

        private void OnDestroy()
        {
            if (Sfx.Player == this)
            {
                Sfx.Player = null;
            }
        }

        public void Play(SoundSet sound, Vector3 position, float gain)
        {
            AudioClip clip = sound.NextClip();
            if (clip == null)
            {
                return;
            }

            if (logPlays)
            {
                Debug.Log($"SFX {clip.name} @{position:0.0} t={Time.time:0.0}");
            }

            int index = FindVoice();
            AudioSource source = sources[index];
            startTimes[index] = Time.unscaledTime;
            source.transform.position = position;
            source.clip = clip;
            source.volume = sound.Volume * gain;
            source.pitch = sound.NextPitch;
            source.spatialBlend = sound.Spatial ? 1f : 0f;
            source.minDistance = sound.MinDistance;
            source.maxDistance = sound.MaxDistance;
            source.Play();
        }

        private int FindVoice()
        {
            int oldest = 0;
            for (int i = 0; i < sources.Length; i++)
            {
                if (!sources[i].isPlaying)
                {
                    return i;
                }

                if (startTimes[i] < startTimes[oldest])
                {
                    oldest = i;
                }
            }

            return oldest;
        }
    }
}
