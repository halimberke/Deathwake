using Game.Localization;
using UnityEngine;

namespace Game.Narrative
{
    // Bir konuşanın kimliği ve sesi. Ses kayıt dosyası yok; anlaşılmaz konuşma bu ayarlarla kodla üretilir.
    [CreateAssetMenu(fileName = "Voice_", menuName = "Game/Speaker Voice")]
    public class SpeakerVoice : ScriptableObject
    {
        [Header("Altyazı")]
        [SerializeField] private string displayName = "ANONS";
        [SerializeField] private Color nameColor = new Color(0.85f, 0.8f, 0.55f);

        [Header("Ses")]
        [Tooltip("Ortalama ses perdesi (Hz). Erkek sesi ~110, kadın sesi ~200.")]
        [SerializeField] private float basePitch = 130f;
        [Tooltip("Heceler arası perde oynaması (yarım ton). Yüksek değer daha dalgalı konuşma.")]
        [SerializeField] private float pitchVariance = 3f;
        [Tooltip("Bir hecenin ortalama süresi (sn). Konuşma hızını belirler.")]
        [SerializeField] private float syllableDuration = 0.11f;
        [Tooltip("Ünlü renklerini kaydırır. 1'den büyük daha ince, küçük daha kalın ses.")]
        [SerializeField] private float formantShift = 1f;
        [Tooltip("Aynı metin farklı konuşanlarda farklı duyulsun diye.")]
        [SerializeField] private int seed = 7;
        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.7f;

        [Header("Hoparlör")]
        [Tooltip("Telsiz/hoparlör filtresi: dar frekans, hafif bozulma ve hışırtı.")]
        [SerializeField] private bool radio = true;
        [Tooltip("Anonstan önce iki tonlu zil çalsın mı.")]
        [SerializeField] private bool chime = true;

        public string DisplayName => Loc.T(displayName);
        public Color NameColor => nameColor;
        public float BasePitch => basePitch;
        public float PitchVariance => pitchVariance;
        public float SyllableDuration => syllableDuration;
        public float FormantShift => formantShift;
        public int Seed => seed;
        public float Volume => volume;
        public bool Radio => radio;
        public bool Chime => chime;
    }
}
