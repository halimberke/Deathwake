using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Flow
{
    // Editor'de bir oda sahnesi doğrudan Play'le açılırsa Core sahnesini de yükler.
    // Böylece her oda tek başına test edilebilir. Build'de Core zaten ilk sahnedir.
    public static class CoreBootstrap
    {
        private const string CoreSceneName = "Core";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureCoreLoaded()
        {
            if (SceneManager.GetSceneByName(CoreSceneName).isLoaded)
            {
                return;
            }

            SceneManager.LoadScene(CoreSceneName, LoadSceneMode.Additive);
        }
    }
}
