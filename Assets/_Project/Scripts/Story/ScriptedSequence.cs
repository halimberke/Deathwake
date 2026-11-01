using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Story
{
    // Hikâye sahnelerinin senaryosu: sırayla çalışan adımlar.
    // Her adım bir koşulu bekler, sonra Inspector'da bağlanan olayları çalıştırır.
    // Örnek: "2 sn bekle → anons", "oyuncu koridora girsin → ışıklar titresin", "telefon açılsın (sinyal) → ses".
    public class ScriptedSequence : MonoBehaviour
    {
        public enum WaitType
        {
            Delay,        // Sadece bekle
            PlayerInZone, // Oyuncu alana girene kadar bekle (daha önce girdiyse beklemez)
            Signal        // Signal("anahtar") çağrılana kadar bekle (örnek: telefon açıldı)
        }

        [Serializable]
        private class Step
        {
            [SerializeField] private string label;
            [SerializeField] private WaitType wait = WaitType.Delay;
            [Tooltip("Delay: beklenecek süre. Diğerlerinde: koşul gerçekleştikten sonraki ek bekleme.")]
            [SerializeField] private float delay;
            [Tooltip("Sadece PlayerInZone için.")]
            [SerializeField] private ScriptZone zone;
            [Tooltip("Sadece Signal için. Bu adım bu anahtarla gelen sinyali bekler.")]
            [SerializeField] private string signalKey;
            [Tooltip("Bu sinyal geldiyse adımın olayları çalışmaz. Örnek: not okunduysa \"masada kâğıt var\" denmez.")]
            [SerializeField] private string skipIfSignal;
            [SerializeField] private UnityEvent action;

            public string Label => label;
            public WaitType Wait => wait;
            public float Delay => delay;
            public ScriptZone Zone => zone;
            public string SignalKey => signalKey;
            public string SkipIfSignal => skipIfSignal;
            public UnityEvent Action => action;
        }

        [Tooltip("Kapalıysa Play() çağrılınca başlar (örnek: RoomRoot'un On Entered olayından).")]
        [SerializeField] private bool playOnStart;
        [SerializeField] private List<Step> steps = new();

        // Gelen sinyaller. Adım ona gelmeden önce verilen sinyal de saklanır ve sayılır.
        // Anahtarlar ayrı tutulur: not telefondan önce okunsa da telefon adımını geçirmez.
        private readonly HashSet<string> signals = new();
        private Coroutine running;

        private void Start()
        {
            if (playOnStart)
            {
                Play();
            }
        }

        public void Play()
        {
            if (running != null)
            {
                StopCoroutine(running);
            }

            signals.Clear();
            running = StartCoroutine(Run());
        }

        public void Signal(string key)
        {
            signals.Add(key);
        }

        // Ölçekli zaman: duraklatınca senaryo da bekler.
        private IEnumerator Run()
        {
            foreach (Step step in steps)
            {
                switch (step.Wait)
                {
                    case WaitType.PlayerInZone:
                        yield return new WaitUntil(() => step.Zone != null && step.Zone.PlayerEntered);
                        break;
                    case WaitType.Signal:
                        yield return new WaitUntil(() => signals.Contains(step.SignalKey));
                        signals.Remove(step.SignalKey);
                        break;
                }

                if (step.Delay > 0f)
                {
                    yield return new WaitForSeconds(step.Delay);
                }

                if (string.IsNullOrEmpty(step.SkipIfSignal) || !signals.Contains(step.SkipIfSignal))
                {
                    step.Action.Invoke();
                }
            }

            running = null;
        }
    }
}
