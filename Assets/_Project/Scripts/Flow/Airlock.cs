using System;
using Game.Audio;
using Game.Echo;
using Game.Interaction;
using UnityEngine;

namespace Game.Flow
{
    // Odalar arasındaki geçiş kabini. Her odanın girişinde durur.
    // Oyuncu içeri girince arkadaki kapı kapanır, kısa bir temizlik işlemi yapılır; önceki oda bu sırada atılır.
    // Temizlik bitince ön kapının kilidi açılır, oyuncu kapıyı kendisi açar (notu okuyabilsin).
    // Ön kapı, oyuncu ve yankılar kapıdan uzaklaşınca bir süre sonra kendiliğinden kapanır ve kilitlenir: geri dönülmez.
    [RequireComponent(typeof(BoxCollider))]
    public class Airlock : MonoBehaviour
    {
        [SerializeField] private Door backDoor;
        [SerializeField] private Door frontDoor;
        [SerializeField] private string playerTag = "Player";

        [Header("Ön kapının kendiliğinden kapanması")]
        [Tooltip("Kabinin içi (yerel). İçeride biri varken kapı kapanmaz.")]
        [SerializeField] private Vector3 interiorCenter = new Vector3(0f, 1.3f, 1.5f);
        [SerializeField] private Vector3 interiorSize = new Vector3(2.4f, 2.6f, 2.9f);
        [Tooltip("Ön kapı açıklığının yerel konumu.")]
        [SerializeField] private Vector3 frontOpening = new Vector3(0f, 0f, 3f);
        [Tooltip("Herkes kapıdan bu kadar uzaklaşınca kapanma sayacı başlar (m). Kapı genişliğinden büyük olmalı.")]
        [SerializeField] private float clearDistance = 2.4f;
        [Tooltip("Uzak kalındıktan sonra kapanmadan önceki bekleme (sn). Geri dönülürse sayaç sıfırlanır.")]
        [SerializeField] private float closeDelay = 1.5f;

        [Header("Temizlik")]
        [SerializeField] private ParticleSystem mist;
        [SerializeField] private AudioSource mistSound;
        [SerializeField] private SoundSet startSound;
        [SerializeField] private SoundSet doneSound;
        [Tooltip("Ön kapının üstündeki durum ışığı: temizlik sürerken bekleme rengi.")]
        [SerializeField] private Renderer statusLight;
        [SerializeField] private Material waitMaterial;
        [SerializeField] private Material readyMaterial;

        // Oyuncu bekleniyorken (arka kapı açık) içeri girerse bir kez tetiklenir.
        public event Action<Airlock> PlayerEntered;

        private bool awaitingPlayer;
        private float clearTime;
        private Transform player;

        public bool FrontIsOpen => frontDoor.IsOpen;

        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        // Önceki odadan gelinecek: arka kapı açık, ön kapı kapalı.
        public void AwaitPlayer()
        {
            awaitingPlayer = true;
            frontDoor.Close();
            frontDoor.Lock();
            backDoor.OpenSilently();
        }

        // Oyunun ilk odası ya da ölümden sonra: arkada bir şey yok, ön kapı açılır.
        public void OpenForStart()
        {
            awaitingPlayer = false;
            backDoor.Close();
            frontDoor.Unlock();
            frontDoor.Open();
        }

        public void CloseBack()
        {
            backDoor.Close();
        }

        public void BeginDecontamination()
        {
            mist.Play();
            mistSound.Play();
            Sfx.Play(startSound, mist.transform.position);
            statusLight.sharedMaterial = waitMaterial;
        }

        public void EndDecontamination()
        {
            mist.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            mistSound.Stop();
            Sfx.Play(doneSound, statusLight.transform.position);
            statusLight.sharedMaterial = readyMaterial;
            frontDoor.Unlock();
        }

        private void Update()
        {
            if (!frontDoor.IsOpen)
            {
                clearTime = 0f;
                return;
            }

            // Oyuncu sahneler arası yaşar; bir kez bulunur.
            if (player == null)
            {
                GameObject found = GameObject.FindWithTag(playerTag);
                player = found != null ? found.transform : null;
            }

            if (player == null || Near(player.position))
            {
                clearTime = 0f;
                return;
            }

            // Kabinde ya da kapının yanında yankı varsa kapı onun içinden geçmesin diye beklenir.
            foreach (GhostActor ghost in GhostActor.Active)
            {
                if (ghost.isActiveAndEnabled && Near(ghost.transform.position))
                {
                    clearTime = 0f;
                    return;
                }
            }

            clearTime += Time.deltaTime;
            if (clearTime >= closeDelay)
            {
                clearTime = 0f;
                frontDoor.Close();
                frontDoor.Lock();
            }
        }

        // Kabinin içinde ya da kapının dönüş alanında.
        private bool Near(Vector3 worldPosition)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            Vector3 fromCenter = local - interiorCenter;
            bool inside = Mathf.Abs(fromCenter.x) <= interiorSize.x * 0.5f && Mathf.Abs(fromCenter.z) <= interiorSize.z * 0.5f;
            Vector2 toOpening = new Vector2(local.x - frontOpening.x, local.z - frontOpening.z);
            return inside || toOpening.magnitude < clearDistance;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!awaitingPlayer || !other.CompareTag(playerTag))
            {
                return;
            }

            awaitingPlayer = false;
            PlayerEntered?.Invoke(this);
        }
    }
}
