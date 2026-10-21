using System;
using Game.Interaction;
using UnityEngine;

namespace Game.Flow
{
    // Odalar arasındaki geçiş kabini. Her odanın girişinde durur.
    // Oyuncu içeri girince arkadaki kapı kapanır; önceki oda bu sırada bellekten atılır.
    // Kabinin görünüşü ve adı (dekontaminasyon, asansör...) koddan bağımsızdır.
    [RequireComponent(typeof(BoxCollider))]
    public class Airlock : MonoBehaviour
    {
        [SerializeField] private Door backDoor;
        [SerializeField] private Door frontDoor;
        [SerializeField] private string playerTag = "Player";

        // Oyuncu bekleniyorken (arka kapı açık) içeri girerse bir kez tetiklenir.
        public event Action<Airlock> PlayerEntered;

        private bool awaitingPlayer;

        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        // Önceki odadan gelinecek: arka kapı açık, ön kapı kapalı.
        public void AwaitPlayer()
        {
            awaitingPlayer = true;
            frontDoor.Close();
            backDoor.Open();
        }

        // Oyunun ilk odası ya da ölümden sonra: arkada bir şey yok, ön kapı açılır.
        public void OpenForStart()
        {
            awaitingPlayer = false;
            backDoor.Close();
            frontDoor.Open();
        }

        public void CloseBack()
        {
            backDoor.Close();
        }

        public void OpenFront()
        {
            frontDoor.Open();
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
