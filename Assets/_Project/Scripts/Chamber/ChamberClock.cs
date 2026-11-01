using System;
using UnityEngine;

namespace Game.Chamber
{
    // Odanın saati. Deneme başlayınca 0'dan sayar.
    // Kayıt ve oynatma bu saati kullanır; böylece oyuncu ve yankılar hep aynı zamandadır.
    public class ChamberClock : MonoBehaviour
    {
        public event Action Expired;

        public float Time { get; private set; }
        public float Duration { get; private set; }
        public bool IsRunning { get; private set; }
        public float Remaining => Mathf.Max(0f, Duration - Time);

        // duration 0: saat sayar (kayıt ve yankı buna bağlı) ama hiç dolmaz.
        public void StartClock(float duration)
        {
            Duration = duration;
            Time = 0f;
            IsRunning = true;
        }

        public void StopClock()
        {
            IsRunning = false;
        }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            Time += UnityEngine.Time.deltaTime;

            if (Duration > 0f && Time >= Duration)
            {
                Time = Duration;
                IsRunning = false;
                Expired?.Invoke();
            }
        }
    }
}
