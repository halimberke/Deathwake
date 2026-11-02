using Game.Audio;
using TMPro;
using UnityEngine;

namespace Game.Chamber
{
    // Odanın duvarındaki dijital geri sayım. Oyunun içinde durur (HUD değil).
    // Son saniyelerde yanıp söner; ölünce 00:00'da kalır, çözülünce yeşile döner.
    public class WallTimer : MonoBehaviour
    {
        [SerializeField] private ChamberController chamber;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Color normalColor = new Color(1f, 0.25f, 0.2f);
        [SerializeField] private Color solvedColor = new Color(0.3f, 1f, 0.4f);
        [Tooltip("Kalan süre bunun altına inince yanıp söner (sn).")]
        [SerializeField] private float warningTime = 10f;
        [Tooltip("Saniyedeki yanıp sönme sayısı.")]
        [SerializeField] private float blinkRate = 2f;
        [Tooltip("Son saniyelerde her saniye çalar.")]
        [SerializeField] private SoundSet tickSound;

        private int lastTick = -1;

        private void Update()
        {
            float remaining = chamber.State switch
            {
                ChamberState.Preparing => chamber.Definition.AttemptDuration,
                ChamberState.Dead => 0f,
                _ => chamber.Clock.Remaining
            };

            // Yukarı yuvarlama: 0.4 sn kalınca 00:01 görünür, 00:00 sadece süre bitince.
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, remaining));
            label.text = $"{seconds / 60:00}:{seconds % 60:00}";

            bool solved = chamber.State == ChamberState.Solved;
            bool blinking = chamber.State == ChamberState.Running && remaining <= warningTime;
            label.color = solved ? solvedColor : normalColor;
            label.enabled = !blinking || Mathf.Repeat(Time.time * blinkRate, 1f) < 0.6f;

            if (blinking && seconds != lastTick && seconds > 0)
            {
                Sfx.Play(tickSound, transform.position);
            }

            lastTick = seconds;
        }
    }
}
