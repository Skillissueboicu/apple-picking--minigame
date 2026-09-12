using UnityEngine.SceneManagement;

namespace FarmerQuest.Core
{
    /// <summary>
    /// Helpers for single-mode scene transitions
    /// </summary>
    public static class SceneFlow
    {
        // Loads a scene by name, replacing the current scene
        public static void Load(string sceneName) =>
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

        public static void GoLogin() => Load(SceneNames.Login);
        public static void GoMainMenu() => Load(SceneNames.MainMenu);
        public static void GoLobby() => Load(SceneNames.Lobby);
        public static void GoFarmerQuestScene() => Load(SceneNames.FarmerQuestScene);
        public static void GoProfile() => Load(SceneNames.Profile);
    }
}
