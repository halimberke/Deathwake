using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.Chamber
{
    // Odaya ilk girişte ekranın üstünde beliren başlık: "DENEY 01 / Bekleyiş".
    // Ölüp yeniden doğunca tekrar çıkmaz.
    [RequireComponent(typeof(CanvasGroup))]
    public class ChamberTitleCard : MonoBehaviour
    {
        [SerializeField] private TMP_Text codeLabel;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private float fadeDuration = 0.8f;
        [SerializeField] private float holdDuration = 2.5f;

        private readonly HashSet<ChamberDefinition> shown = new();
        private CanvasGroup group;
        private Coroutine routine;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
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
            if (chamber == null || !shown.Add(chamber.Definition))
            {
                return;
            }

            codeLabel.text = chamber.Definition.TitleCode;
            nameLabel.text = chamber.Definition.DisplayName;

            if (routine != null)
            {
                StopCoroutine(routine);
            }

            routine = StartCoroutine(Show());
        }

        // Ölçekli zaman: duraklatınca kart da bekler, hızlandırınca çabuk geçer.
        private IEnumerator Show()
        {
            yield return Fade(0f, 1f);
            yield return new WaitForSeconds(holdDuration);
            yield return Fade(1f, 0f);
            routine = null;
        }

        private IEnumerator Fade(float from, float to)
        {
            for (float t = 0f; t < fadeDuration; t += Time.deltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / fadeDuration);
                yield return null;
            }

            group.alpha = to;
        }
    }
}
