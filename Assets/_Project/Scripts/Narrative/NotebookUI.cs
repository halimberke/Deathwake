using System.Collections;
using System.Collections.Generic;
using Game.Audio;
using Game.Chamber;
using Game.Flow;
using Game.Localization;
using Game.Player;
using Game.Settings;
using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Narrative
{
    // İki görünüm: dünyadaki notu okurken tek kâğıt, Tab ile defter (solda başlıklar, sağda sayfa).
    // İkisi de ekranın ortasında; dünya etrafta görünmeye devam eder. Oda saati durmaz.
    public class NotebookUI : MonoBehaviour
    {
        [Header("Girdi")]
        [SerializeField] private InputActionReference notebookAction;
        [Tooltip("Esc. Açık paneli kapatır.")]
        [SerializeField] private InputActionReference closeAction;
        [Tooltip("E. Okunan notu kapatır.")]
        [SerializeField] private InputActionReference interactAction;
        [SerializeField] private PlayerRig player;

        [Header("Not okuma")]
        [SerializeField] private GameObject readerPanel;
        [SerializeField] private Image readerPaper;
        [SerializeField] private TMP_Text readerTitle;
        [SerializeField] private TMP_Text readerBody;
        [Tooltip("El yazısı notun kâğıdı bu kadar eğik durur (derece).")]
        [SerializeField] private float handwrittenTilt = -2f;
        [SerializeField] private TMP_Text readerHint;
        [Tooltip("{0} = etkileşim tuşu, {1} = defter tuşu.")]
        [SerializeField] private string readerHintFormat = "{0} / {1}: kapat";

        [Header("Defter")]
        [SerializeField] private GameObject notebookPanel;
        [SerializeField] private Button entryTemplate;
        [SerializeField] private TMP_Text emptyLabel;
        [SerializeField] private Image pagePaper;
        [SerializeField] private TMP_Text pageTitle;
        [SerializeField] private TMP_Text pageBody;
        [SerializeField] private TMP_Text notebookHint;
        [Tooltip("{0} = defter tuşu.")]
        [SerializeField] private string notebookHintFormat = "{0}: kapat";

        [Header("Bildirim")]
        [SerializeField] private CanvasGroup addedToast;
        [SerializeField] private TMP_Text addedToastLabel;
        [Tooltip("{0} = defter tuşu.")]
        [SerializeField] private string addedToastFormat = "Deftere eklendi   [{0}]";
        [SerializeField] private float toastDuration = 2.5f;

        [Header("Ses")]
        [SerializeField] private SoundSet openSound;
        [SerializeField] private SoundSet closeSound;
        [Tooltip("Deftere yeni not eklenince.")]
        [SerializeField] private SoundSet addedSound;

        [Header("Kâğıt ve mürekkep")]
        [SerializeField] private Color typedPaper = new Color(0.86f, 0.85f, 0.8f);
        [SerializeField] private Color typedInk = new Color(0.1f, 0.1f, 0.1f);
        [SerializeField] private Color handwrittenPaper = new Color(0.82f, 0.78f, 0.66f);
        [SerializeField] private Color handwrittenInk = new Color(0.12f, 0.16f, 0.35f);
        [Tooltip("Daktilo notlar: resmî belge görünümü.")]
        [SerializeField] private TMP_FontAsset typedFont;
        [SerializeField] private TMP_FontAsset handwrittenFont;

        // Duraklatma menüsü bakar: panel açıkken ya da bu karede kapandıysa Esc menüyü açmaz.
        public static bool BlocksPause => IsOpen || closedFrame == Time.frameCount;
        public static bool IsOpen { get; private set; }

        private static int closedFrame = -1;

        private readonly List<Button> entries = new();
        private readonly Dictionary<NoteDefinition, MenuItem> entryItems = new();
        private int openedFrame = -1;
        private bool toastPending;
        private Coroutine toastRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            IsOpen = false;
            closedFrame = -1;
        }

        private void Awake()
        {
            readerPanel.SetActive(false);
            notebookPanel.SetActive(false);
            entryTemplate.gameObject.SetActive(false);
            addedToast.alpha = 0f;
        }

        private void OnEnable()
        {
            notebookAction.action.Enable();
            Notebook.ReadRequested += HandleReadRequested;
            ActiveChamber.Changed += HandleChamberChanged;
        }

        private void OnDisable()
        {
            notebookAction.action.Disable();
            Notebook.ReadRequested -= HandleReadRequested;
            ActiveChamber.Changed -= HandleChamberChanged;
        }

        private void Update()
        {
            if (GamePause.IsPaused || MainMenu.IsOpen)
            {
                return;
            }

            if (!IsOpen)
            {
                if (notebookAction.action.WasPressedThisFrame())
                {
                    OpenNotebook();
                }

                return;
            }

            // Açan tuş aynı karede kapatmasın.
            if (Time.frameCount == openedFrame)
            {
                return;
            }

            if (notebookAction.action.WasPressedThisFrame() || closeAction.action.WasPressedThisFrame() ||
                interactAction.action.WasPressedThisFrame())
            {
                Close();
            }
        }

        private void HandleReadRequested(NoteDefinition note, bool isNew)
        {
            toastPending |= isNew;
            readerPanel.SetActive(true);
            notebookPanel.SetActive(false);
            ApplyPaper(readerPaper, readerTitle, readerBody, note);
            readerHint.text = Loc.Format(readerHintFormat,
                KeyLabels.Get(interactAction.action), KeyLabels.Get(notebookAction.action));
            readerPaper.rectTransform.localRotation =
                Quaternion.Euler(0f, 0f, note.Style == NoteStyle.Handwritten ? handwrittenTilt : 0f);
            SetOpen(true);
        }

        // Oda değişince ya da ölüm sonrası oda yeniden yüklenince açık panel kapanır.
        private void HandleChamberChanged(ChamberController chamber)
        {
            if (IsOpen)
            {
                Close();
            }
        }

        private void OpenNotebook()
        {
            RebuildEntries();
            IReadOnlyList<NoteDefinition> notes = Notebook.Notes;
            emptyLabel.gameObject.SetActive(notes.Count == 0);
            pagePaper.gameObject.SetActive(notes.Count > 0);
            if (notes.Count > 0)
            {
                ShowPage(notes[notes.Count - 1]);
            }

            notebookHint.text = Loc.Format(notebookHintFormat, KeyLabels.Get(notebookAction.action));
            readerPanel.SetActive(false);
            notebookPanel.SetActive(true);
            Sfx.Play(openSound, transform.position);
            SetOpen(true);
        }

        private void RebuildEntries()
        {
            foreach (Button entry in entries)
            {
                Destroy(entry.gameObject);
            }

            entries.Clear();
            entryItems.Clear();

            foreach (NoteDefinition note in Notebook.Notes)
            {
                Button entry = Instantiate(entryTemplate, entryTemplate.transform.parent);
                entry.gameObject.SetActive(true);
                entry.GetComponentInChildren<TMP_Text>().text = note.Title;
                entry.onClick.AddListener(() => ShowPage(note));
                entries.Add(entry);
                entryItems[note] = entry.GetComponent<MenuItem>();
            }
        }

        private void ShowPage(NoteDefinition note)
        {
            ApplyPaper(pagePaper, pageTitle, pageBody, note);

            // Açık sayfanın başlığı listede vurgulu kalır.
            foreach (KeyValuePair<NoteDefinition, MenuItem> pair in entryItems)
            {
                pair.Value.SetTabActive(pair.Key == note);
            }
        }

        private void ApplyPaper(Image paper, TMP_Text title, TMP_Text body, NoteDefinition note)
        {
            bool handwritten = note.Style == NoteStyle.Handwritten;
            paper.color = handwritten ? handwrittenPaper : typedPaper;
            Color ink = handwritten ? handwrittenInk : typedInk;
            FontStyles style = handwritten ? FontStyles.Italic : FontStyles.Normal;

            TMP_FontAsset font = handwritten ? handwrittenFont : typedFont;
            title.font = font;
            body.font = font;

            title.text = note.Title;
            title.color = ink;
            title.fontStyle = style | FontStyles.Bold;
            body.text = note.Body;
            body.color = ink;
            body.fontStyle = style;
        }

        private void Close()
        {
            Sfx.Play(closeSound, transform.position);
            readerPanel.SetActive(false);
            notebookPanel.SetActive(false);
            SetOpen(false);

            if (toastPending)
            {
                toastPending = false;
                Sfx.Play(addedSound, transform.position);
                addedToastLabel.text = Loc.Format(addedToastFormat, KeyLabels.Get(notebookAction.action));
                if (toastRoutine != null)
                {
                    StopCoroutine(toastRoutine);
                }

                toastRoutine = StartCoroutine(ShowToast());
            }
        }

        private void SetOpen(bool open)
        {
            if (open)
            {
                openedFrame = Time.frameCount;
            }
            else
            {
                closedFrame = Time.frameCount;
            }

            IsOpen = open;
            player.SetReading(open);

            // Son tıklanan başlık "seçili" renginde kalmasın.
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private IEnumerator ShowToast()
        {
            for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
            {
                addedToast.alpha = t / 0.2f;
                yield return null;
            }

            addedToast.alpha = 1f;
            yield return new WaitForSecondsRealtime(toastDuration);

            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                addedToast.alpha = 1f - t / 0.5f;
                yield return null;
            }

            addedToast.alpha = 0f;
            toastRoutine = null;
        }
    }
}
