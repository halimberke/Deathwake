using Game.Interaction;
using UnityEngine;

namespace Game.Narrative
{
    // Dünyada duran not. Oyuncu E ile okur; yankı dokunmaz ve okuma kayda girmez.
    public class NoteItem : MonoBehaviour, IInteractable, IPlayerOnly
    {
        [SerializeField] private NoteDefinition note;
        [SerializeField] private string prompt = "Oku";

        public string GetPrompt(IInteractor interactor) => prompt;

        public bool CanInteract(IInteractor interactor) => interactor is PlayerInteractor;

        public void Interact(IInteractor interactor)
        {
            if (interactor is PlayerInteractor)
            {
                Notebook.Read(note);
            }
        }
    }
}
