using System;
using Game.Audio;
using Game.Chamber;
using UnityEngine;

namespace Game.Player
{
    // Oyuncunun canı. Hasar alınca azalır, bir süre hasar almazsa dolar. Sıfırlanınca aktif oda öldürür.
    // Yeni bir odaya girildiğinde ya da yeniden doğulduğunda (aktif oda değişince) can tamamen dolar.
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [Tooltip("Son hasardan sonra iyileşme başlamadan önceki bekleme (sn).")]
        [SerializeField] private float regenDelay = 1.5f;
        [Tooltip("İyileşme hızı (can/sn).")]
        [SerializeField] private float regenRate = 40f;
        [SerializeField] private SoundSet hitSound;

        public float Current { get; private set; }
        public float Normalized => Current / maxHealth;

        // Hasar alındığında (arayüz geri bildirimi için).
        public event Action Damaged;

        private float timeSinceDamage;

        private void Awake()
        {
            Current = maxHealth;
        }

        private void OnEnable()
        {
            ActiveChamber.Changed += HandleChamberChanged;
        }

        private void OnDisable()
        {
            ActiveChamber.Changed -= HandleChamberChanged;
        }

        private void HandleChamberChanged(ChamberController chamber)
        {
            Current = maxHealth;
            timeSinceDamage = regenDelay;
        }

        public void TakeDamage(float amount, string deathReason)
        {
            ChamberController chamber = ActiveChamber.Current;
            if (chamber == null || chamber.State != ChamberState.Running || Current <= 0f)
            {
                return;
            }

            Current = Mathf.Max(0f, Current - amount);
            timeSinceDamage = 0f;
            Sfx.Play(hitSound, transform.position);
            Damaged?.Invoke();

            if (Current <= 0f)
            {
                chamber.Kill(deathReason);
            }
        }

        private void Update()
        {
            timeSinceDamage += Time.deltaTime;
            if (timeSinceDamage >= regenDelay && Current > 0f)
            {
                Current = Mathf.Min(maxHealth, Current + regenRate * Time.deltaTime);
            }
        }
    }
}
