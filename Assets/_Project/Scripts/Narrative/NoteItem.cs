using Game.Audio;
using Game.Interaction;
using Game.Localization;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Narrative
{
    // Dünyada duran not. Oyuncu E ile okur; yankı dokunmaz ve okuma kayda girmez.
    public class NoteItem : MonoBehaviour, IInteractable, IPlayerOnly
    {
        [SerializeField] private NoteDefinition note;
        [SerializeField] private string prompt = "Oku";
        [SerializeField] private SoundSet readSound;
        [Tooltip("Not her okunduğunda. Senaryo buradan sinyal alabilir.")]
        [SerializeField] private UnityEvent onRead;

        public string GetPrompt(IInteractor interactor) => Loc.T(prompt);

        public bool CanInteract(IInteractor interactor) => interactor is PlayerInteractor;

        public void Interact(IInteractor interactor)
        {
            if (interactor is PlayerInteractor)
            {
                Sfx.Play(readSound, transform.position);
                Notebook.Read(note);
                onRead.Invoke();
            }
        }
    }
}
