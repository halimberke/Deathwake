using System;
using UnityEngine;

namespace Game.Interaction
{
    // Oyuncunun tek eşyalık eli. Envanter yok: yeni eşya alınınca eldeki bırakılır.
    public class HandSlot : MonoBehaviour
    {
        [Tooltip("Eşyanın elde durduğu nokta. Kameranın alt nesnesi olmalı.")]
        [SerializeField] private Transform holdPoint;
        [Tooltip("Bırakma yönü ve engel kontrolü için. Genelde ana kamera.")]
        [SerializeField] private Transform dropOrigin;
        [SerializeField] private float dropDistance = 0.8f;
        [Tooltip("Önde duvar varsa eşya duvardan bu kadar uzağa bırakılır.")]
        [SerializeField] private float wallPadding = 0.2f;
        [SerializeField] private LayerMask obstacleMask = ~0;

        // Eldeki eşya değişince (alma, bırakma, verme). Null = el boş. Örnek: ağır eşya oyuncuyu yavaşlatır.
        public event Action<Pickup> HeldItemChanged;

        public Pickup HeldItem { get; private set; }

        public bool IsHolding(ItemDefinition item)
        {
            return HeldItem != null && item != null && HeldItem.Definition == item;
        }

        public void Hold(Pickup item)
        {
            if (HeldItem != null)
            {
                // Takas: eldeki, yeni eşyanın durduğu yere bırakılır.
                Pickup previous = HeldItem;
                previous.Release(item.transform.position, item.transform.rotation);
            }

            item.AttachTo(holdPoint);
            SetHeld(item);
        }

        // Eldeki eşyayı başka bir sisteme verir (örnek: geçiş kutusu). Eşyayı nereye koyacağı alana kalır.
        public Pickup Take()
        {
            Pickup item = HeldItem;
            SetHeld(null);
            return item;
        }

        public void Drop()
        {
            if (HeldItem == null)
            {
                return;
            }

            Pickup item = HeldItem;
            SetHeld(null);
            item.Release(GetDropPosition(), Quaternion.Euler(0f, dropOrigin.eulerAngles.y, 0f));
        }

        private void SetHeld(Pickup item)
        {
            if (HeldItem == item)
            {
                return;
            }

            HeldItem = item;
            HeldItemChanged?.Invoke(item);
        }

        // Duvarın içine bırakmamak için önce önüne ışın atılır.
        private Vector3 GetDropPosition()
        {
            Vector3 direction = dropOrigin.forward;
            float distance = dropDistance;

            if (Physics.Raycast(dropOrigin.position, direction, out RaycastHit hit, dropDistance + wallPadding,
                    obstacleMask, QueryTriggerInteraction.Ignore))
            {
                distance = Mathf.Max(0f, hit.distance - wallPadding);
            }

            return dropOrigin.position + direction * distance;
        }
    }
}
