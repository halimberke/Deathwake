using Game.Audio;
using Game.Localization;
using UnityEngine;

namespace Game.Interaction
{
    // Bir eşya türünün verisi. Sahnedeki nesne değil, Project penceresindeki bir asset'tir.
    // Kapı gibi nesneler "hangi eşyayı istiyorum" sorusunu bu asset'e referansla cevaplar.
    [CreateAssetMenu(fileName = "Item_", menuName = "Game/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Eşya";
        [Tooltip("Taşırken hız çarpanı. 1 = normal, 0.5 = yarı hız (ağır eşya).")]
        [Range(0.1f, 1f)]
        [SerializeField] private float carrySpeedMultiplier = 1f;
        [SerializeField] private SoundSet pickupSound;
        [Tooltip("Konunca ve fizikle bir yere çarpınca.")]
        [SerializeField] private SoundSet placeSound;

        public string DisplayName => Loc.T(displayName);
        public float CarrySpeedMultiplier => carrySpeedMultiplier;
        public SoundSet PickupSound => pickupSound;
        public SoundSet PlaceSound => placeSound;
    }
}
