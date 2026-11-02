using Game.Audio;
using Game.Visual;
using UnityEngine;

namespace Game.Chamber
{
    // Sürenin son saniyelerinde nozullar gaz püskürtür, oda hızla dolar. Süre bitince oda öldürür
    // (ChamberDefinition.TimeoutReason). Bu bileşen sadece görüntüyü ve sesi yönetir; ölüm kararı saatte.
    public class GasRelease : MonoBehaviour
    {
        // Herhangi bir odada gaz püskürmeye başlayınca. Karakterin tepkisi buradan gelir.
        public static event System.Action Released;

        [SerializeField] private ChamberController chamber;
        [Tooltip("Süre bitmeden kaç saniye önce başlar.")]
        [SerializeField] private float releaseLead = 8f;
        [Tooltip("Püskürten bütün parçacık sistemleri (jet ve bulut).")]
        [SerializeField] private ParticleSystem[] emitters;
        [SerializeField] private ProceduralSound hiss;
        [Tooltip("Salım başlarken çalan uyarı.")]
        [SerializeField] private SoundSet alarm;
        [SerializeField] private Color gasColor = new Color(0.62f, 0.65f, 0.68f);

        private RetroAtmosphere atmosphere;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Released = null;
        }
        private bool released;

        private void OnEnable()
        {
            chamber.StateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            chamber.StateChanged -= HandleStateChanged;
            Stop();
        }

        private void HandleStateChanged(ChamberState previous, ChamberState next)
        {
            if (next == ChamberState.Running && atmosphere == null)
            {
                atmosphere = FindAnyObjectByType<RetroAtmosphere>();
            }

            if (next == ChamberState.Solved)
            {
                Stop();
            }
        }

        private void Update()
        {
            if (chamber.State != ChamberState.Running)
            {
                return;
            }

            float remaining = chamber.Clock.Remaining;
            if (remaining > releaseLead)
            {
                return;
            }

            if (!released)
            {
                Release();
            }

            // Sis saatten hesaplanır: duraklama ve hızlandırma doğru çalışır.
            float coverage = 1f - remaining / releaseLead;
            atmosphere.SetGas(coverage, gasColor);
        }

        private void Release()
        {
            released = true;
            foreach (ParticleSystem emitter in emitters)
            {
                emitter.Play();
            }

            hiss.Play();
            Sfx.Play(alarm, transform.position);
            Released?.Invoke();
        }

        private void Stop()
        {
            if (!released)
            {
                return;
            }

            released = false;
            foreach (ParticleSystem emitter in emitters)
            {
                emitter.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            hiss.Stop();
            if (atmosphere != null)
            {
                atmosphere.SetGas(0f, gasColor);
            }
        }
    }
}
