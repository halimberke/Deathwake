using System.Collections;
using Game.Audio;
using Game.Chamber;
using Game.Visual;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Player
{
    // Ölüm anı: kamera yere yığılır, ekran kırmızı parlar ve griye döner, kulak çınlar.
    // Karartmayı ve odanın yeniden yüklenmesini GameFlow yapar; bu sadece hissi verir.
    public class DeathEffect : MonoBehaviour
    {
        [SerializeField] private PlayerRig player;
        [SerializeField] private RetroScreen screen;
        [SerializeField] private Image redFlash;
        [SerializeField] private ProceduralSound tinnitus;

        [Header("Süreler (sn)")]
        [SerializeField] private float fallDuration = 0.7f;
        [SerializeField] private float desaturateDuration = 0.6f;
        [Tooltip("Kırmızı parlamanın başlangıç ve kalıcı opaklığı.")]
        [SerializeField] private Vector2 flashAlpha = new Vector2(0.55f, 0.15f);

        private ChamberController chamber;
        private Coroutine routine;

        private void Awake()
        {
            SetFlash(0f);
        }

        private void OnEnable()
        {
            ActiveChamber.Changed += HandleChamberChanged;
            HandleChamberChanged(ActiveChamber.Current);
        }

        private void OnDisable()
        {
            ActiveChamber.Changed -= HandleChamberChanged;
            HandleChamberChanged(null);
        }

        // Oda değişince (ölümden sonra yeniden yüklenince de) efekt sıfırlanır ve yeni oda dinlenir.
        private void HandleChamberChanged(ChamberController next)
        {
            if (chamber != null)
            {
                chamber.StateChanged -= HandleStateChanged;
            }

            chamber = next;
            if (chamber != null)
            {
                chamber.StateChanged += HandleStateChanged;
            }

            Clear();
        }

        private void HandleStateChanged(ChamberState previous, ChamberState next)
        {
            if (next == ChamberState.Dead)
            {
                player.PlayDeath(fallDuration);
                tinnitus.Play();
                routine = StartCoroutine(Play());
            }
        }

        // Ölçeksiz zaman: hızlandırma açıkken ölünse de efekt aynı hızda.
        private IEnumerator Play()
        {
            for (float t = 0f; t < desaturateDuration; t += Time.unscaledDeltaTime)
            {
                float k = t / desaturateDuration;
                screen.SetDesaturation(k);
                SetFlash(Mathf.Lerp(flashAlpha.x, flashAlpha.y, k));
                yield return null;
            }

            screen.SetDesaturation(1f);
            SetFlash(flashAlpha.y);
            routine = null;
        }

        private void Clear()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            if (screen != null)
            {
                screen.SetDesaturation(0f);
            }

            SetFlash(0f);
        }

        private void SetFlash(float alpha)
        {
            Color color = redFlash.color;
            color.a = alpha;
            redFlash.color = color;
        }
    }
}
