using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Game.Chamber
{
    // Odanın durum makinesi: Hazırlık → Deneme → Ölüm → Sıfırlama, ya da Deneme → Çözüldü.
    // Her geçişte olay yayınlar. Kayıt, yankı, arayüz gibi sistemler bu olayları dinler.
    [RequireComponent(typeof(ChamberClock))]
    public class ChamberController : MonoBehaviour
    {
        [SerializeField] private ChamberDefinition definition;
        [Tooltip("Sahne yüklendikten sonra deneme başlamadan önceki bekleme (saniye).")]
        [SerializeField] private float prepareDuration = 0.5f;
        [Tooltip("Ölümden sonra sahne yeniden yüklenmeden önceki bekleme (saniye).")]
        [SerializeField] private float deathDuration = 1.5f;

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

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(prepareDuration);
            BeginAttempt();
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
            StartCoroutine(ReloadAfterDeath());
        }

        // Çıkış kapısı gibi bir nesne odanın çözüldüğünü bildirir.
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

        // "Odayı sıfırla": tüm denemeleri ve yankıları silip baştan başlar.
        public void ResetChamber()
        {
            if (State == ChamberState.Resetting)
            {
                return;
            }

            ChamberSession.Clear(definition);
            Reload();
        }

        private void BeginAttempt()
        {
            Progress.BeginAttempt();
            Clock.StartClock(definition.AttemptDuration);
            SetState(ChamberState.Running);
            onAttemptStarted.Invoke();
        }

        private void HandleClockExpired()
        {
            Kill("Süre doldu");
        }

        private IEnumerator ReloadAfterDeath()
        {
            yield return new WaitForSeconds(deathDuration);
            Reload();
        }

        private void Reload()
        {
            SetState(ChamberState.Resetting);
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex);
        }

        private void SetState(ChamberState next)
        {
            ChamberState previous = State;
            State = next;
            StateChanged?.Invoke(previous, next);
        }
    }
}
