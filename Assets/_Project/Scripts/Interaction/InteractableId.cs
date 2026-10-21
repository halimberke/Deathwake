using System;
using UnityEngine;

namespace Game.Interaction
{
    // Sahne yeniden yüklense de değişmeyen kimlik. Yankı "hangi kapı?" sorusunu bununla cevaplar.
    // IInteractable bileşeniyle aynı nesneye eklenir.
    [DisallowMultipleComponent]
    public class InteractableId : MonoBehaviour
    {
        [SerializeField] private string id;

        public string Id => id;

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(id))
            {
                InteractableRegistry.Register(this);
            }
        }

        private void OnDisable()
        {
            InteractableRegistry.Unregister(this);
        }

#if UNITY_EDITOR
        // Bileşen ilk eklendiğinde çağrılır.
        private void Reset()
        {
            id = NewId();
        }

        // Inspector'da bir şey değişince ya da nesne kopyalanınca çağrılır.
        // Ctrl+D ile kopyalanan nesne aynı kimliği taşır; burada yenisi verilir.
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (string.IsNullOrEmpty(id) || HasDuplicateInScene())
            {
                id = NewId();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        private bool HasDuplicateInScene()
        {
            foreach (InteractableId other in FindObjectsByType<InteractableId>(FindObjectsInactive.Include))
            {
                if (other != this && other.id == id)
                {
                    return true;
                }
            }

            return false;
        }

        private static string NewId()
        {
            return Guid.NewGuid().ToString("N");
        }
#endif
    }
}
