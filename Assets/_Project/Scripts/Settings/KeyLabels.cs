using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Settings
{
    // Bir eylemin o an atanmış tuşunun ekrandaki adı ("E", "Left Shift").
    // Yazılar her karede sorulabilir: sonuç önbellekte durur, tuş değişince temizlenir.
    public static class KeyLabels
    {
        public const string KeyboardGroup = "Keyboard&Mouse";

        private static readonly Dictionary<InputAction, string> cache = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            cache.Clear();
        }

        public static string Get(InputAction action)
        {
            if (action == null)
            {
                return string.Empty;
            }

            if (!cache.TryGetValue(action, out string label))
            {
                label = action.GetBindingDisplayString(InputBinding.MaskByGroup(KeyboardGroup),
                    InputBinding.DisplayStringOptions.DontIncludeInteractions);
                cache[action] = label;
            }

            return label;
        }

        public static void Invalidate()
        {
            cache.Clear();
        }
    }
}
