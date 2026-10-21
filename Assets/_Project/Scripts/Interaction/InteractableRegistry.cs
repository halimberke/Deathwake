using System.Collections.Generic;
using UnityEngine;

namespace Game.Interaction
{
    // Çalışma anında kimlikten nesneye ulaşmak için sözlük. Nesneler kendilerini kaydeder.
    public static class InteractableRegistry
    {
        private static readonly Dictionary<string, InteractableId> byId = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            byId.Clear();
        }

        public static void Register(InteractableId item)
        {
            if (byId.TryGetValue(item.Id, out InteractableId existing) && existing != null && existing != item)
            {
                Debug.LogError($"Aynı kimliğe sahip iki nesne: {existing.name} ve {item.name}", item);
            }

            byId[item.Id] = item;
        }

        public static void Unregister(InteractableId item)
        {
            if (!string.IsNullOrEmpty(item.Id) && byId.TryGetValue(item.Id, out InteractableId existing) && existing == item)
            {
                byId.Remove(item.Id);
            }
        }

        public static bool TryGet(string id, out InteractableId item)
        {
            return byId.TryGetValue(id, out item) && item != null;
        }
    }
}
