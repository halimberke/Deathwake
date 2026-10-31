using System;
using System.Collections.Generic;
using System.IO;
using Game.Narrative;
using UnityEngine;

namespace Game.Flow
{
    // Kayıt: son girilen sahne ve defter. Yankılar ve eşyalar kaydedilmez, sahne baştan başlar.
    // Dosya JSON'dur (persistentDataPath/save.json). Editor'de bir oda doğrudan açılınca Recording
    // kapalı kalır: test oynamak kayıtlı oyunu bozmaz.
    public class GameSave : MonoBehaviour
    {
        [Serializable]
        private class Data
        {
            public string sceneName;
            public List<string> notes = new();
        }

        [SerializeField] private NoteCatalog noteCatalog;

        public bool Recording { get; set; }

        private string currentScene;

        private static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

        public bool HasSave => File.Exists(FilePath);

        private void OnEnable()
        {
            Notebook.Added += Write;
        }

        private void OnDisable()
        {
            Notebook.Added -= Write;
        }

        public void SetScene(string sceneName)
        {
            currentScene = sceneName;
            Write();
        }

        public void Delete()
        {
            if (HasSave)
            {
                File.Delete(FilePath);
            }
        }

        // Defteri geri yükler ve son sahnenin adını verir. Dosya bozuksa false.
        public bool TryLoad(out string sceneName)
        {
            sceneName = null;
            if (!HasSave)
            {
                return false;
            }

            try
            {
                var data = JsonUtility.FromJson<Data>(File.ReadAllText(FilePath));
                if (data == null || string.IsNullOrEmpty(data.sceneName))
                {
                    return false;
                }

                var notes = new List<NoteDefinition>();
                foreach (string id in data.notes)
                {
                    NoteDefinition note = noteCatalog.Find(id);
                    if (note != null)
                    {
                        notes.Add(note);
                    }
                }

                Notebook.Restore(notes);
                sceneName = data.sceneName;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Kayıt okunamadı: {exception.Message}", this);
                return false;
            }
        }

        private void Write()
        {
            if (!Recording || string.IsNullOrEmpty(currentScene))
            {
                return;
            }

            var data = new Data { sceneName = currentScene };
            foreach (NoteDefinition note in Notebook.Notes)
            {
                data.notes.Add(note.name);
            }

            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
        }
    }
}
