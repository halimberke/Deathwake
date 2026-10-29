using System.Collections;
using UnityEngine;

namespace Game.Visual
{
    // Açılıp kapanabilen ve titreyebilen lamba. Senaryo adımları bu metotları çağırır.
    [RequireComponent(typeof(Light))]
    public class FlickerLight : MonoBehaviour
    {
        [SerializeField] private bool startOn = true;
        [Tooltip("Titreme sırasında iki değişim arasındaki en kısa ve en uzun süre (sn).")]
        [SerializeField] private Vector2 flickerInterval = new Vector2(0.03f, 0.15f);

        private Light lamp;
        private float baseIntensity;
        private bool isOn;
        private Coroutine flicker;

        private void Awake()
        {
            lamp = GetComponent<Light>();
            baseIntensity = lamp.intensity;
            SetOn(startOn);
        }

        public void TurnOn() => SetOn(true);

        public void TurnOff() => SetOn(false);

        public void SetOn(bool on)
        {
            StopFlicker();
            isOn = on;
            lamp.enabled = on;
            lamp.intensity = baseIntensity;
        }

        // Verilen süre boyunca rastgele söner ve zayıflar, sonra önceki hâline döner.
        public void Flicker(float duration)
        {
            StopFlicker();
            flicker = StartCoroutine(FlickerRoutine(duration));
        }

        private IEnumerator FlickerRoutine(float duration)
        {
            float end = Time.time + duration;
            while (Time.time < end)
            {
                lamp.enabled = Random.value > 0.35f;
                lamp.intensity = baseIntensity * Random.Range(0.2f, 1f);
                yield return new WaitForSeconds(Random.Range(flickerInterval.x, flickerInterval.y));
            }

            flicker = null;
            SetOn(isOn);
        }

        private void StopFlicker()
        {
            if (flicker != null)
            {
                StopCoroutine(flicker);
                flicker = null;
            }
        }
    }
}
