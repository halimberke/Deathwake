using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Chamber
{
    // Odanın durum makinesi: Hazırlık → Deneme → Ölüm, ya da Deneme → Çözüldü.
    // Her geçişte olay yayınlar. Kayıt, yankı, arayüz gibi sistemler bu olayları dinler.
    // Sahne yükleme ve yeniden doğma bu sınıfın işi değil; oyun akışı (GameFlow) yapar.
    [RequireComponent(typeof(ChamberClock))]
    public class ChamberController : MonoBehaviour
    {
        [SerializeField] private ChamberDefinition definition;
        [Tooltip("Begin çağrıldıktan sonra deneme başlamadan önceki bekleme (saniye).")]
        [SerializeField] private float prepareDuration = 0.5f;

        [Header("Olaylar")]
        [SerializeField] private UnityEvent onAttemptStarted;
        [SerializeField] private UnityEvent onDied;
        [SerializeField] private UnityEvent onSolved;

        // Kodla dinleyenler için. Önceki ve yeni durum.
        public event Action<ChamberState, ChamberState> StateChanged;

        public ChamberState State { get; private set; } = ChamberState.Preparing;
        public ChamberDefinition Definition => definition;
        public ChamberClock Clock { get; private set; }
        public ChamberProgress Progress { get; private set; }
        public string LastDeathReason { get; private set; }

        private bool begun;

        private void Awake()
        {
            Clock = GetComponent<ChamberClock>();
            Progress = ChamberSession.Get(definition);
        }

        private void OnEnable()
        {
            Clock.Expired += HandleClockExpired;
        }

        private void OnDisable()
        {
            Clock.Expired -= HandleClockExpired;
        }

        // Oyun akışı, oyuncu odaya girince çağırır. Oda yüklendiği anda değil:
        // sonraki oda arka planda yüklenip bekleyebilir.
        public void Begin()
        {
            if (begun)
            {
                return;
            }

            begun = true;
            StartCoroutine(BeginAfterPrepare());
        }

        // Tehlikeler (gaz vb.) oyuncuyu öldürürken bunu çağırır.
        public void Kill(string reason)
        {
            if (State != ChamberState.Running)
            {
                return;
            }

            LastDeathReason = reason;
            Clock.StopClock();
            SetState(ChamberState.Dead);
            onDied.Invoke();
        }

        // Çıkış alanı gibi bir nesne odanın çözüldüğünü bildirir.
        public void Solve()
        {
            if (State != ChamberState.Running)
            {
                return;
            }

            Clock.StopClock();
            SetState(ChamberState.Solved);
            onSolved.Invoke();
        }

        private IEnumerator BeginAfterPrepare()
        {
            yield return new WaitForSeconds(prepareDuration);

            Progress.BeginAttempt();
            Clock.StartClock(definition.AttemptDuration);
            SetState(ChamberState.Running);
            onAttemptStarted.Invoke();
        }

        private void HandleClockExpired()
        {
            Kill("Süre doldu");
        }

        private void SetState(ChamberState next)
        {
            ChamberState previous = State;
            State = next;
            StateChanged?.Invoke(previous, next);
        }
    }
}
