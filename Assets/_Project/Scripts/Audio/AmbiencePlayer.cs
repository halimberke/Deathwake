using System.Collections;
using UnityEngine;

namespace Game.Audio
{
    // Core'da yaşar: odanın sürekli ortam sesini çalar. İki kaynak arasında yavaşça geçer.
    // Ses odada değil burada çalar: oda arka planda yüklenirken iki ortam üst üste binmez,
    // ölümde oda yeniden yüklenince aynı ses kesilip baştan başlamaz.
    public class AmbiencePlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource first;
        [SerializeField] private AudioSource second;
        [Tooltip("Ortamlar arası geçiş süresi (sn).")]
        [SerializeField] private float crossfade = 2.5f;

        private AudioSource active;
        private Coroutine fading;

        private void Awake()
        {
            active = first;
        }

        // clip boşsa ortam susar (ev gibi kendi sesini çalan sahneler).
        public void Play(AudioClip clip, float volume)
        {
            if (active.clip == clip && (clip == null || active.isPlaying))
            {
                return;
            }

            AudioSource next = active == first ? second : first;
            next.clip = clip;
            next.volume = 0f;
            if (clip != null)
            {
                next.Play();
            }

            if (fading != null)
            {
                StopCoroutine(fading);
            }

            fading = StartCoroutine(Crossfade(active, next, clip != null ? volume : 0f));
            active = next;
        }

        // Ölçeksiz zaman: oda hızlandırılsa da geçiş aynı sürer.
        private IEnumerator Crossfade(AudioSource from, AudioSource to, float targetVolume)
        {
            float startVolume = from.volume;
            for (float t = 0f; t < crossfade; t += Time.unscaledDeltaTime)
            {
                float k = t / crossfade;
                from.volume = startVolume * (1f - k);
                to.volume = targetVolume * k;
                yield return null;
            }

            from.Stop();
            from.clip = null;
            from.volume = 0f;
            to.volume = targetVolume;
            fading = null;
        }
    }
}
