using UnityEngine;

namespace Game.Chamber
{
    // Bir odanın verisi. Yeni oda eklemek için bu asset'ten bir tane oluşturulur.
    [CreateAssetMenu(fileName = "Chamber_", menuName = "Game/Chamber Definition")]
    public class ChamberDefinition : ScriptableObject
    {
        [Tooltip("Başlık kartının üst satırı. Örnek: DENEY 01")]
        [SerializeField] private string titleCode = "DENEY 00";
        [SerializeField] private string displayName = "Oda";
        [Tooltip("Bir denemenin süresi (saniye). Süre dolunca oda öldürür.")]
        [SerializeField] private float attemptDuration = 45f;
        [Tooltip("Aynı anda en fazla kaç yankı olabilir. Aşılınca en eskisi silinir.")]
        [SerializeField] private int maxEchoes = 1;

        public string TitleCode => titleCode;
        public string DisplayName => displayName;
        public float AttemptDuration => attemptDuration;
        public int MaxEchoes => maxEchoes;
    }
}
