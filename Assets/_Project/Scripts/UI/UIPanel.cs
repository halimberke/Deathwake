using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    // Açılıp kapanan ekran parçası: solarak ve hafifçe kayarak gelir, öğeleri sırayla belirir,
    // açıkken klavye odağını korur. Süreler ölçeksiz: duraklatmada da çalışır.
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPanel : MonoBehaviour
    {
        [SerializeField] private UITheme theme;
        [Tooltip("Kayarak gelen parça. Boşsa panel sadece solar.")]
        [SerializeField] private RectTransform slideTarget;
        [Tooltip("Açılışta odaklanılan öğe.")]
        [SerializeField] private Selectable defaultSelectable;
        [Tooltip("Sırayla belirecek öğeler.")]
        [SerializeField] private MenuItem[] staggeredItems;

        public bool IsOpen { get; private set; }

        private CanvasGroup group;
        private Coroutine transition;
        private GameObject lastFocus;
        private Vector2 slideRest;
        private bool ready;

        // Panel sahnede kapalı başlar; Awake ilk açılışa kadar çalışmaz.
        private void EnsureReady()
        {
            if (ready)
            {
                return;
            }

            ready = true;
            group = GetComponent<CanvasGroup>();
            if (slideTarget != null)
            {
                slideRest = slideTarget.anchoredPosition;
            }
        }

        public void Show(Selectable focus = null)
        {
            EnsureReady();
            gameObject.SetActive(true);
            IsOpen = true;
            group.interactable = true;
            group.blocksRaycasts = true;
            lastFocus = null;

            Selectable first = focus != null ? focus : defaultSelectable;
            if (first != null && first.isActiveAndEnabled)
            {
                UIFocus.Select(first.gameObject);
            }

            for (int i = 0; staggeredItems != null && i < staggeredItems.Length; i++)
            {
                if (staggeredItems[i] != null && staggeredItems[i].isActiveAndEnabled)
                {
                    staggeredItems[i].PlayIntro(i * theme.StaggerDelay);
                }
            }

            Run(Fade(1f, theme.PanelSlide, null));
        }

        public void Hide(Action done = null)
        {
            EnsureReady();
            IsOpen = false;
            group.interactable = false;
            group.blocksRaycasts = false;

            if (!isActiveAndEnabled)
            {
                done?.Invoke();
                return;
            }

            Run(Fade(0f, -theme.PanelSlide * 0.5f, () =>
            {
                gameObject.SetActive(false);
                done?.Invoke();
            }));
        }

        public void HideImmediate()
        {
            EnsureReady();
            IsOpen = false;
            if (transition != null)
            {
                StopCoroutine(transition);
                transition = null;
            }

            group.alpha = 0f;
            gameObject.SetActive(false);
        }

        // Üstünde modal pencere açıkken bu panele tıklanamaz, klavye de buraya gelmez.
        public void SetInteractable(bool value)
        {
            EnsureReady();
            group.interactable = value;
            group.blocksRaycasts = value;
        }

        private void Update()
        {
            if (!IsOpen || !group.interactable)
            {
                return;
            }

            // Boşluğa tıklayınca seçim kaybolur; klavye çalışmaya devam etsin diye geri alınır.
            GameObject current = UIFocus.Current;
            if (current != null && current.activeInHierarchy)
            {
                if (current.transform.IsChildOf(transform))
                {
                    lastFocus = current;
                }

                return;
            }

            GameObject target = lastFocus != null && lastFocus.activeInHierarchy
                ? lastFocus
                : defaultSelectable != null ? defaultSelectable.gameObject : null;
            if (target != null)
            {
                UIFocus.Select(target);
            }
        }

        private void Run(IEnumerator routine)
        {
            if (transition != null)
            {
                StopCoroutine(transition);
            }

            transition = StartCoroutine(routine);
        }

        private IEnumerator Fade(float target, float slide, Action done)
        {
            float start = target > 0.5f ? 0f : group.alpha;
            Vector2 from = slideTarget != null && target > 0.5f ? slideRest + Vector2.right * -slide : slideRest;
            Vector2 to = slideTarget != null && target > 0.5f ? slideRest : slideRest + Vector2.right * slide;

            for (float t = 0f; t < theme.PanelDuration; t += Time.unscaledDeltaTime)
            {
                float k = UITheme.EaseOut(t / theme.PanelDuration);
                group.alpha = Mathf.Lerp(start, target, k);
                if (slideTarget != null)
                {
                    slideTarget.anchoredPosition = Vector2.Lerp(from, to, k);
                }

                yield return null;
            }

            group.alpha = target;
            if (slideTarget != null)
            {
                slideTarget.anchoredPosition = slideRest;
            }

            transition = null;
            done?.Invoke();
        }
    }
}
