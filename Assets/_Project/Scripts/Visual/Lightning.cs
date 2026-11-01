using System.Collections;
using Game.Audio;
using UnityEngine;

namespace Game.Visual
{
    // Şimşek: pencerelerin parlaması (genel shader değişkeni), kısa ışık çakması ve gecikmeli gök gürültüsü.
    // Kendi başına ara ara çakar; senaryo Strike ile hemen çaktırabilir.
    public class Lightning : MonoBehaviour
    {
        private static readonly int FlashId = Shader.PropertyToID("_RainFlash");

        [SerializeField] private Light[] flashLights;
        [SerializeField] private ProceduralSound thunder;
        [SerializeField] private Vector2 interval = new Vector2(18f, 40f);
        [Tooltip("Çakma ile gürültü arasındaki gecikme (uzaklık hissi).")]
        [SerializeField] private Vector2 thunderDelay = new Vector2(0.6f, 2.2f);

        private float[] baseIntensity;
        private Coroutine strike;

        private void Awake()
        {
            baseIntensity = new float[flashLights.Length];
            for (int i = 0; i < flashLights.Length; i++)
            {
                baseIntensity[i] = flashLights[i].intensity;
                flashLights[i].enabled = false;
            }
        }

        private void OnEnable()
        {
            StartCoroutine(Loop());
        }

        private void OnDisable()
        {
            Shader.SetGlobalFloat(FlashId, 0f);
        }

        public void Strike()
        {
            if (strike != null)
            {
                StopCoroutine(strike);
            }

            strike = StartCoroutine(StrikeRoutine());
        }

        private IEnumerator Loop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(interval.x, interval.y));
                Strike();
            }
        }

        private IEnumerator StrikeRoutine()
        {
            // İki ya da üç hızlı çakma, sonra sönüm.
            int flashes = Random.Range(2, 4);
            for (int i = 0; i < flashes; i++)
            {
                SetFlash(Random.Range(0.6f, 1f));
                yield return new WaitForSeconds(Random.Range(0.04f, 0.09f));
                SetFlash(0.1f);
                yield return new WaitForSeconds(Random.Range(0.05f, 0.14f));
            }

            float fade = 0.1f;
            while (fade > 0f)
            {
                fade -= Time.deltaTime * 0.3f;
                SetFlash(Mathf.Max(fade, 0f));
                yield return null;
            }

            strike = null;
            StartCoroutine(ThunderRoutine());
        }

        private void SetFlash(float amount)
        {
            Shader.SetGlobalFloat(FlashId, amount);
            for (int i = 0; i < flashLights.Length; i++)
            {
                flashLights[i].enabled = amount > 0f;
                flashLights[i].intensity = baseIntensity[i] * amount;
            }
        }

        private IEnumerator ThunderRoutine()
        {
            yield return new WaitForSeconds(Random.Range(thunderDelay.x, thunderDelay.y));
            thunder.Play();
        }
    }
}
