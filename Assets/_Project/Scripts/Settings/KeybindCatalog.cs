using System;
using System.Collections.Generic;
using Game.Localization;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Settings
{
    // Ayarlar ekranındaki tuş listesi. Oyuna yeni eylem eklenince buraya bir satır eklenir;
    // kod ve arayüz değişmez, satır ekranda kendiliğinden belirir.
    [CreateAssetMenu(fileName = "KeybindCatalog", menuName = "Game/Keybind Catalog")]
    public class KeybindCatalog : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [SerializeField] private string label = "Eylem";
            [SerializeField] private InputActionReference action;
            [Tooltip("Birleşik eylemin parçası (Move için up, down, left, right). Tek tuşlu eylemde boş.")]
            [SerializeField] private string compositePart;

            public string Label => Loc.T(label);
            public InputAction Action => action != null ? action.action : null;

            // Klavye grubundaki ilk uygun bağlantının sırası. Bulunamazsa -1.
            public int FindBindingIndex()
            {
                InputAction target = Action;
                if (target == null)
                {
                    return -1;
                }

                for (int i = 0; i < target.bindings.Count; i++)
                {
                    InputBinding binding = target.bindings[i];
                    if (binding.isComposite || binding.groups == null ||
                        !binding.groups.Contains(KeyLabels.KeyboardGroup))
                    {
                        continue;
                    }

                    bool partMatches = string.IsNullOrEmpty(compositePart)
                        ? !binding.isPartOfComposite
                        : binding.isPartOfComposite && binding.name == compositePart;
                    if (partMatches)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        [SerializeField] private List<Entry> entries = new();

        public IReadOnlyList<Entry> Entries => entries;
    }
}
