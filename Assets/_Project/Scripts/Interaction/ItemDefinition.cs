using UnityEngine;

namespace Game.Interaction
{
    // Bir eşya türünün verisi. Sahnedeki nesne değil, Project penceresindeki bir asset'tir.
    // Kapı gibi nesneler "hangi eşyayı istiyorum" sorusunu bu asset'e referansla cevaplar.
    [CreateAssetMenu(fileName = "Item_", menuName = "Game/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Eşya";

        public string DisplayName => displayName;
    }
}
