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
        [Tooltip("Lamba yanarken kendi ışığı açılan yüzeyler (abajur, tavan lambası camı).")]
        [SerializeField] private Renderer[] glowRenderers;
        [SerializeField] private Color glowColor = new Color(1f, 0.82f, 0.55f);

        private static readonly int EmissionId = Shader.PropertyToID("_Emission");

        private Light lamp;
        private float baseIntensity;
        private bool isOn;
        private Coroutine flicker;
        private MaterialPropertyBlock glowBlock;

        private void Awake()
        {
            lamp = GetComponent<Light>();
            baseIntensity = lamp.intensity;
            SetOn(startOn);
        }

        public void TurnOn() => SetOn(true);

        public void TurnOff() => SetOn(false);

        public void Toggle() => SetOn(!isOn);

        public void SetOn(bool on)
        {
            StopFlicker();
            isOn = on;
            lamp.enabled = on;
            lamp.intensity = baseIntensity;
            SetGlow(on ? 1f : 0f);
        }

        // Her Renderer için ayrı malzeme kopyası açmamak için MaterialPropertyBlock kullanılır.
        private void SetGlow(float amount)
        {
            if (glowRenderers == null || glowRenderers.Length == 0)
            {
                return;
            }

            glowBlock ??= new MaterialPropertyBlock();
            foreach (Renderer glow in glowRenderers)
            {
                glow.GetPropertyBlock(glowBlock);
                glowBlock.SetColor(EmissionId, glowColor * amount);
                glow.SetPropertyBlock(glowBlock);
            }
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
                SetGlow(lamp.enabled ? lamp.intensity / baseIntensity : 0f);
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
