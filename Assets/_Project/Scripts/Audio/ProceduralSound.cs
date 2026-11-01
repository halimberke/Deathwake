using UnityEngine;

namespace Game.Audio
{
    // Sahnedeki bir ses kaynağı: sesini SfxSynth ile kendisi üretir.
    // Konumlu (3D) çalar: telefon zili telefonun olduğu yerden duyulur.
    [RequireComponent(typeof(AudioSource))]
    public class ProceduralSound : MonoBehaviour
    {
        [SerializeField] private SfxType type;
        [SerializeField] private bool loop;
        [Tooltip("Açıksa çaldığı yerden duyulur (uzaklaşınca kısılır). Kapalıysa her yerden aynı.")]
        [SerializeField] private bool spatial = true;
        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.8f;
        [Tooltip("Konumlu seste, bu mesafeye kadar ses kısılmaz. Evin her yerinden duyulması gereken sesler için büyütülür.")]
        [SerializeField] private float minDistance = 1.5f;
        [SerializeField] private bool playOnStart;
        [Tooltip("Oyun duraklatılınca susmasın (arayüz sesleri).")]
        [SerializeField] private bool ignoreListenerPause;

        private AudioSource source;
        private AudioClip clip;

        private void Awake()
        {
            EnsureReady();
        }

        // Başka bir bileşen bunu Awake'ten önce çalmak isteyebilir (aynı karede oluşan nesneler).
        private void EnsureReady()
        {
            if (source != null)
            {
                return;
            }

            clip = SfxSynth.Create(type);
            source = GetComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.volume = volume;
            source.playOnAwake = false;
            source.ignoreListenerPause = ignoreListenerPause;
            source.spatialBlend = spatial ? 1f : 0f;
            source.minDistance = minDistance;
            source.maxDistance = 15f;
        }

        private void Start()
        {
            if (playOnStart)
            {
                Play();
            }
        }

        private void OnDestroy()
        {
            Destroy(clip);
        }

        public void Play()
        {
            EnsureReady();
            source.Play();
        }

        public void Stop()
        {
            EnsureReady();
            source.Stop();
        }
    }
}
