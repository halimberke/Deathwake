using System;
using UnityEngine;

namespace Game.Audio
{
    // Tek bir sesin bütün çeşitleri ve çalma ayarları. Her çalışta farklı çeşit ve hafif farklı perde seçilir:
    // aynı ses art arda geldiğinde makineli tüfek gibi tekrar etmesin.
    [CreateAssetMenu(fileName = "Sound_", menuName = "Game/Sound Set")]
    public class SoundSet : ScriptableObject
    {
        [SerializeField] private AudioClip[] clips;
        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.7f;
        [Tooltip("Rastgele perde aralığı. 1 = kaydedildiği gibi.")]
        [SerializeField] private Vector2 pitchRange = new Vector2(0.96f, 1.04f);

        [Header("Konum")]
        [Tooltip("Açıksa çaldığı yerden duyulur. Kapalıysa her yerden aynı (arayüz, oyuncunun kendi vücudu).")]
        [SerializeField] private bool spatial = true;
        [Tooltip("Bu mesafeye kadar ses kısılmaz.")]
        [SerializeField] private float minDistance = 2f;
        [Tooltip("Bu mesafeden sonra duyulmaz.")]
        [SerializeField] private float maxDistance = 22f;

        [NonSerialized] private int lastIndex = -1;

        public float Volume => volume;
        public bool Spatial => spatial;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;
        public float NextPitch => UnityEngine.Random.Range(pitchRange.x, pitchRange.y);

        // Bir öncekiyle aynı çeşit iki kez üst üste gelmez.
        public AudioClip NextClip()
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            int index = UnityEngine.Random.Range(0, clips.Length);
            if (clips.Length > 1 && index == lastIndex)
            {
                index = (index + 1) % clips.Length;
            }

            lastIndex = index;
            return clips[index];
        }
    }
}
