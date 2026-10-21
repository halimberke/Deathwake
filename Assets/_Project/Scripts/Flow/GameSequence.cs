using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Flow
{
    // Oyunun sırası: odalar ve ara sahneler. Akış kodda değil bu listede durur.
    // Bir odayı öne almak ya da araya sahne eklemek için liste düzenlenir.
    [CreateAssetMenu(fileName = "GameSequence", menuName = "Game/Game Sequence")]
    public class GameSequence : ScriptableObject
    {
        public enum StepType
        {
            Room,
            Cutscene // Henüz desteklenmiyor; akış bu adımları atlar.
        }

        [Serializable]
        public class Step
        {
            [SerializeField] private StepType type = StepType.Room;
            [Tooltip("Sahnenin adı (uzantısız). Build Settings'te olmalı.")]
            [SerializeField] private string sceneName;

            public StepType Type => type;
            public string SceneName => sceneName;
        }

        [SerializeField] private List<Step> steps = new();

        public int Count => steps.Count;
        public Step this[int index] => steps[index];

        public int IndexOfScene(string sceneName)
        {
            return steps.FindIndex(step => step.SceneName == sceneName);
        }
    }
}
