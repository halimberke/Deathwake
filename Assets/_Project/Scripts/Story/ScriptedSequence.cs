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
            PlayerInZone, // Oyuncu alana girene kadar bekle
            Signal        // Signal() çağrılana kadar bekle (örnek: bir nesneyle etkileşim)
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
            [SerializeField] private UnityEvent action;

            public string Label => label;
            public WaitType Wait => wait;
            public float Delay => delay;
            public ScriptZone Zone => zone;
            public UnityEvent Action => action;
        }

        [Tooltip("Kapalıysa Play() çağrılınca başlar (örnek: RoomRoot'un On Entered olayından).")]
        [SerializeField] private bool playOnStart;
        [SerializeField] private List<Step> steps = new();

        // Sinyal, adım ona gelmeden önce verildiyse de sayılır.
        private bool signaled;
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

            signaled = false;
            running = StartCoroutine(Run());
        }

        public void Signal()
        {
            signaled = true;
        }

        // Ölçekli zaman: duraklatınca senaryo da bekler.
        private IEnumerator Run()
        {
            foreach (Step step in steps)
            {
                switch (step.Wait)
                {
                    case WaitType.PlayerInZone:
                        yield return new WaitUntil(() => step.Zone != null && step.Zone.PlayerInside);
                        break;
                    case WaitType.Signal:
                        yield return new WaitUntil(() => signaled);
                        signaled = false;
                        break;
                }

                if (step.Delay > 0f)
                {
                    yield return new WaitForSeconds(step.Delay);
                }

                step.Action.Invoke();
            }

            running = null;
        }
    }
}
