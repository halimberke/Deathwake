using System;
using System.Collections.Generic;
using Game.Localization;
using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Settings
{
    // Ayarlar ekranı: solda sekmeler (Genel, Görüntü, Kontroller), sağda satırlar.
    // Ana menü ve duraklatma menüsü aynı paneli açar. Tuş satırları katalogdan çalışma anında üretilir.
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private UIPanel panel;
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private MenuItem backItem;

        [Header("Sekmeler (aynı sırada)")]
        [SerializeField] private MenuItem[] tabs;
        [SerializeField] private UIPanel[] tabPages;

        [Header("Genel")]
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private TMP_Text sensitivityValue;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TMP_Text volumeValue;
        [SerializeField] private Button languagePrevious;
        [SerializeField] private Button languageNext;
        [SerializeField] private TMP_Text languageValue;

        [Header("Görüntü")]
        [SerializeField] private ToggleSwitch fullscreenToggle;
        [SerializeField] private TMP_Text fullscreenValue;
        [SerializeField] private Button resolutionPrevious;
        [SerializeField] private Button resolutionNext;
        [SerializeField] private TMP_Text resolutionValue;

        [Header("Kontroller")]
        [SerializeField] private KeybindCatalog catalog;
        [SerializeField] private KeybindRow rowTemplate;
        [SerializeField] private ScrollRect keybindScroll;
        [SerializeField] private Button resetBindingsButton;
        [SerializeField] private TMP_Text bindingHint;
        [SerializeField] private string defaultHint = "Değiştirmek için tuşa tıkla.";
        [SerializeField] private string waitingText = "Bir tuşa bas";
        [SerializeField] private string cancelHint = "Esc ile iptal et.";
        [Tooltip("{0} = diğer eylem. Tuş çakışınca gösterilir.")]
        [SerializeField] private string swappedHint = "Tuş, {0} ile yer değiştirdi.";

        public bool IsOpen => panel.IsOpen;
        public bool IsRebinding => rebinding != null;

        // Atamayı iptal eden Esc aynı anda paneli de kapatmasın.
        public bool BlocksClose => IsRebinding || Time.frameCount <= rebindEndFrame + 1;

        public event Action Closed;

        private readonly List<KeybindRow> rows = new();
        private readonly List<Vector2Int> resolutions = new();
        private InputActionRebindingExtensions.RebindingOperation rebinding;
        private int resolutionIndex;
        private int rebindEndFrame = -10;
        private GameObject lastRevealed;

        private void Start()
        {
            rowTemplate.gameObject.SetActive(false);

            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                tabs[i].Button.onClick.AddListener(() => ShowTab(index));
            }

            sensitivitySlider.minValue = GameSettings.MinSensitivity;
            sensitivitySlider.maxValue = GameSettings.MaxSensitivity;
            sensitivitySlider.onValueChanged.AddListener(GameSettings.SetSensitivity);
            sensitivitySlider.onValueChanged.AddListener(_ => RefreshValues());
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.onValueChanged.AddListener(GameSettings.SetVolume);
            volumeSlider.onValueChanged.AddListener(_ => RefreshValues());

            fullscreenToggle.Changed += HandleFullscreenChanged;
            resolutionPrevious.onClick.AddListener(() => StepResolution(-1));
            resolutionNext.onClick.AddListener(() => StepResolution(1));
            resolutionPrevious.onClick.AddListener(UIAudio.Click);
            resolutionNext.onClick.AddListener(UIAudio.Click);
            languagePrevious.onClick.AddListener(() => StepLanguage(-1));
            languageNext.onClick.AddListener(() => StepLanguage(1));
            languagePrevious.onClick.AddListener(UIAudio.Click);
            languageNext.onClick.AddListener(UIAudio.Click);
            Loc.Changed += HandleLanguageChanged;
            resetBindingsButton.onClick.AddListener(ResetBindings);
            resetBindingsButton.onClick.AddListener(UIAudio.Click);
            backItem.Button.onClick.AddListener(Close);

            BuildResolutions();
            BuildRows();
        }

        private void OnDestroy()
        {
            Loc.Changed -= HandleLanguageChanged;
            CancelRebinding();
        }

        public void Open()
        {
            panel.Show(tabs[0].Button);
            ShowTab(0);
            sensitivitySlider.SetValueWithoutNotify(GameSettings.Sensitivity);
            volumeSlider.SetValueWithoutNotify(GameSettings.Volume);
            fullscreenToggle.SetValueWithoutNotify(GameSettings.Fullscreen);
            bindingHint.text = Loc.T(defaultHint);
            RefreshValues();
            RefreshRows();
        }

        public void Close()
        {
            CancelRebinding();
            GameSettings.Save();
            panel.Hide();
            Closed?.Invoke();
        }

        private void ShowTab(int index)
        {
            for (int i = 0; i < tabPages.Length; i++)
            {
                tabs[i].SetTabActive(i == index);
                if (i == index)
                {
                    tabPages[i].Show(tabs[i].Button);
                }
                else
                {
                    tabPages[i].HideImmediate();
                }
            }

            keybindScroll.verticalNormalizedPosition = 1f;
        }

        private void RefreshValues()
        {
            sensitivityValue.text = $"{GameSettings.Sensitivity / GameSettings.DefaultSensitivity:0.0}x";
            volumeValue.text = Loc.Format("%{0}", Mathf.RoundToInt(GameSettings.Volume * 100f));
            languageValue.text = Loc.NameOf(Loc.Current);
            fullscreenValue.text = Loc.T(GameSettings.Fullscreen ? "Açık" : "Kapalı");
            resolutionValue.text = resolutions.Count > 0
                ? $"{resolutions[resolutionIndex].x} × {resolutions[resolutionIndex].y}"
                : "-";
        }

        private void StepLanguage(int step)
        {
            IReadOnlyList<string> languages = Loc.Languages;
            int index = 0;
            while (index < languages.Count && languages[index] != Loc.Current)
            {
                index++;
            }

            GameSettings.SetLanguage(languages[(index + step + languages.Count) % languages.Count]);
        }

        // Sabit yazılar LocalizedText ile kendini yeniler; burada koddan yazılanlar kalır.
        private void HandleLanguageChanged()
        {
            bindingHint.text = Loc.T(defaultHint);
            RefreshValues();
            RefreshRows();
        }

        private void HandleFullscreenChanged(bool value)
        {
            GameSettings.SetFullscreen(value);
            RefreshValues();
        }

        private void BuildResolutions()
        {
            // Aynı boyut farklı yenileme hızlarıyla birkaç kez listelenir; tekilleştirilir.
            foreach (Resolution resolution in Screen.resolutions)
            {
                var size = new Vector2Int(resolution.width, resolution.height);
                if (!resolutions.Contains(size))
                {
                    resolutions.Add(size);
                }
            }

            int width = GameSettings.Width > 0 ? GameSettings.Width : Display.main.systemWidth;
            int height = GameSettings.Height > 0 ? GameSettings.Height : Display.main.systemHeight;
            int found = resolutions.IndexOf(new Vector2Int(width, height));
            resolutionIndex = found >= 0 ? found : Mathf.Max(0, resolutions.Count - 1);
        }

        private void StepResolution(int step)
        {
            if (resolutions.Count == 0)
            {
                return;
            }

            resolutionIndex = (resolutionIndex + step + resolutions.Count) % resolutions.Count;
            Vector2Int size = resolutions[resolutionIndex];
            GameSettings.SetResolution(size.x, size.y);
            RefreshValues();
        }

        private void BuildRows()
        {
            foreach (KeybindCatalog.Entry entry in catalog.Entries)
            {
                KeybindRow row = Instantiate(rowTemplate, rowTemplate.transform.parent);
                row.gameObject.SetActive(true);
                row.Init(entry, StartRebinding);
                rows.Add(row);
            }
        }

        private void RefreshRows()
        {
            foreach (KeybindRow row in rows)
            {
                row.Refresh();
            }
        }

        private void Update()
        {
            RevealSelectedRow();
        }

        // Klavyeyle gezinirken seçili satır görünür alanın dışına çıkmasın.
        private void RevealSelectedRow()
        {
            GameObject current = UIFocus.Current;
            if (current == lastRevealed || current == null || !current.transform.IsChildOf(keybindScroll.content))
            {
                lastRevealed = current;
                return;
            }

            lastRevealed = current;
            var rowCorners = new Vector3[4];
            var viewCorners = new Vector3[4];
            current.GetComponent<RectTransform>().GetWorldCorners(rowCorners);
            keybindScroll.viewport.GetWorldCorners(viewCorners);

            float above = rowCorners[1].y - viewCorners[1].y;
            float below = viewCorners[0].y - rowCorners[0].y;
            float shift = above > 0f ? -above : below > 0f ? below : 0f;
            keybindScroll.content.position += Vector3.up * shift;
        }

        private void StartRebinding(KeybindRow row)
        {
            if (rebinding != null || row.BindingIndex < 0)
            {
                return;
            }

            InputAction action = row.Action;
            bool wasEnabled = action.enabled;
            string oldPath = action.bindings[row.BindingIndex].effectivePath;

            // Atama sırasında eylem kapalı olmalı; arayüz de basılan tuşla gezinmemeli.
            action.Disable();
            UIFocus.SetNavigation(false);
            row.ShowWaiting(Loc.T(waitingText));
            bindingHint.text = Loc.T(cancelHint);

            rebinding = action.PerformInteractiveRebinding(row.BindingIndex)
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsExcluding("<Mouse>/scroll")
                .WithControlsExcluding("<Mouse>/leftButton") // Arayüz tıklamaları için ayrılmış
                .WithControlsExcluding("<Pointer>/press")
                .WithControlsExcluding("<Gamepad>")
                .WithControlsExcluding("<Keyboard>/anyKey")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(operation => FinishRebinding(row, oldPath, wasEnabled, true))
                .OnCancel(operation => FinishRebinding(row, oldPath, wasEnabled, false))
                .Start();
        }

        private void FinishRebinding(KeybindRow row, string oldPath, bool wasEnabled, bool completed)
        {
            rebinding.Dispose();
            rebinding = null;
            rebindEndFrame = Time.frameCount;
            UIFocus.SetNavigation(true);
            if (wasEnabled)
            {
                row.Action.Enable();
            }

            bindingHint.text = Loc.T(defaultHint);
            if (completed)
            {
                string newPath = row.Action.bindings[row.BindingIndex].effectivePath;
                KeybindRow other = FindConflict(row, newPath);
                if (other != null)
                {
                    // Aynı tuş iki eyleme bağlı kalmasın: diğer eylem bu eylemin eski tuşunu alır.
                    other.Action.ApplyBindingOverride(other.BindingIndex, oldPath);
                    bindingHint.text = Loc.Format(swappedHint, other.Entry.Label);
                }

                SaveBindings();
            }

            RefreshRows();
        }

        private KeybindRow FindConflict(KeybindRow changed, string path)
        {
            foreach (KeybindRow row in rows)
            {
                if (row != changed && row.BindingIndex >= 0 &&
                    string.Equals(row.Action.bindings[row.BindingIndex].effectivePath, path,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return row;
                }
            }

            return null;
        }

        private void ResetBindings()
        {
            actions.RemoveAllBindingOverrides();
            SaveBindings();
            RefreshRows();
            bindingHint.text = Loc.T(defaultHint);
        }

        private void SaveBindings()
        {
            GameSettings.SetBindingOverrides(actions.SaveBindingOverridesAsJson());
            GameSettings.Save();
            KeyLabels.Invalidate();
        }

        private void CancelRebinding()
        {
            rebinding?.Cancel();
        }
    }
}
