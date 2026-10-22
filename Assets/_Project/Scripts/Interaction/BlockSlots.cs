using UnityEngine;

namespace Game.Interaction
{
    // Blokların takıldığı yuva dizisi (örnek: platforma çıkan basamaklar).
    // Blok elindeyken E'ye basınca ilk boş yuvaya yerleşir; sıra önemli değil.
    // Fiziksel istifleme yerine sabit yuvalar: her denemede aynı sonuç (yankı tekrarı bozulmaz).
    public class BlockSlots : MonoBehaviour, IInteractable
    {
        [Tooltip("Kabul edilen eşya türü.")]
        [SerializeField] private ItemDefinition acceptedItem;
        [Tooltip("Yerleşme noktaları, dolma sırasıyla.")]
        [SerializeField] private Transform[] slots;
        [SerializeField] private string prompt = "Yerleştir";
        [SerializeField] private string idlePrompt = "Basamak yuvası";
        [SerializeField] private string fullPrompt = "Yuvalar dolu";

        private Pickup[] placed;

        private void Awake()
        {
            placed = new Pickup[slots.Length];
        }

        public string GetPrompt(IInteractor interactor)
        {
            if (FirstFreeSlot() < 0)
            {
                return fullPrompt;
            }

            return CanInteract(interactor) ? $"{prompt} ({interactor.Hand.HeldItem.DisplayName})" : idlePrompt;
        }

        public bool CanInteract(IInteractor interactor)
        {
            return FirstFreeSlot() >= 0 && interactor.Hand != null && interactor.Hand.IsHolding(acceptedItem);
        }

        public void Interact(IInteractor interactor)
        {
            int index = FirstFreeSlot();
            Pickup item = interactor.Hand.Take();
            item.Place(slots[index]);
            placed[index] = item;
        }

        private int FirstFreeSlot()
        {
            for (int i = 0; i < placed.Length; i++)
            {
                if (placed[i] == null)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
