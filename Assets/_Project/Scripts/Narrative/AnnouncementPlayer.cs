using System.Collections;
using System.Collections.Generic;
using Game.Flow;
using UnityEngine;

namespace Game.Narrative
{
    // Core'da yaşar: anonsları sırayla çalar ve altyazıyı sesle eş zamanlı açar.
    // Altyazı sesin çalma konumuna (source.time) bakar; duraklatınca ses de yazı da durur.
    [RequireComponent(typeof(AudioSource))]
    public class AnnouncementPlayer : MonoBehaviour
    {
        [SerializeField] private SubtitleUI subtitles;
        [Tooltip("Son satır bittikten sonra altyazının ekranda kalma süresi (sn).")]
        [SerializeField] private float lingerDuration = 1.2f;

        private readonly Queue<AnnouncementDefinition> queue = new();
        private AudioSource source;
        private Coroutine playing;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // Hoparlör her yerden duyulur.
        }

        private void OnEnable()
        {
            Announcer.Requested += Enqueue;
            Announcer.Stopped += StopAll;
        }

        private void OnDisable()
        {
            Announcer.Requested -= Enqueue;
            Announcer.Stopped -= StopAll;
        }

        private void Enqueue(AnnouncementDefinition announcement)
        {
            queue.Enqueue(announcement);
            playing ??= StartCoroutine(PlayQueue());
        }

        private void StopAll()
        {
            queue.Clear();
            if (playing != null)
            {
                StopCoroutine(playing);
                playing = null;
            }

            if (source.clip != null)
            {
                source.Stop();
                Destroy(source.clip);
                source.clip = null;
            }

            subtitles.Hide();
        }

        private IEnumerator PlayQueue()
        {
            while (queue.Count > 0)
            {
                AnnouncementDefinition announcement = queue.Dequeue();
                SpeakerVoice voice = announcement.Speaker;

                if (voice.Chime)
                {
                    AudioClip chime = GibberishSynth.Chime(voice);
                    yield return PlayClip(chime, null);
                    Destroy(chime);
                }

                foreach (string line in announcement.Lines)
                {
                    SpokenLine spoken = GibberishSynth.Speak(line, voice);
                    subtitles.Show(voice.DisplayName, voice.NameColor, line);
                    yield return PlayClip(spoken.Clip, spoken);
                    subtitles.SetVisibleCharacters(line.Length);
                    Destroy(spoken.Clip);
                    yield return WaitUnpaused(announcement.LinePause);
                }

                yield return WaitUnpaused(lingerDuration);
                if (queue.Count == 0)
                {
                    subtitles.Hide();
                }
            }

            playing = null;
        }

        private IEnumerator PlayClip(AudioClip clip, SpokenLine spoken)
        {
            source.clip = clip;
            source.Play();

            while (GamePause.IsPaused || source.isPlaying)
            {
                if (spoken != null)
                {
                    subtitles.SetVisibleCharacters(spoken.VisibleCharactersAt(source.time));
                }

                yield return null;
            }

            source.clip = null;
        }

        // Gerçek saniye bekler, duraklatma sürerken saymaz.
        private static IEnumerator WaitUnpaused(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                if (!GamePause.IsPaused)
                {
                    elapsed += Time.unscaledDeltaTime;
                }

                yield return null;
            }
        }
    }
}
