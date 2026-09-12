using FarmerQuest.Core;
using FarmerQuest.Net;
using UnityEngine;

namespace FarmerQuest.UI
{
    /// <summary>
    /// Boot scene - auto-login, or go to Login
    /// </summary>
    public sealed class BootController : MonoBehaviour
    {
        private async void Start()
        {
            App.Init();

            // AppHost must live on Boot (DontDestroyOnLoad) for session polling
            if (AppHost.EnsureHost() == null)
                Debug.LogError("[Boot] AppHost missing. Add an AppHost GameObject to Boot.unity, and start Play from Boot.");

            GameContext.Session = null;
            GameContext.CurrentSession = null;

            // Already have a valid token
            if (App.Auth != null && App.Auth.IsAuthenticated)
            {
                SceneFlow.GoMainMenu();
                return;
            }

            // Try remembered credentials
            if (App.Auth != null && App.Auth.TryGetSavedCredentials(out string email, out string password))
            {
                try
                {
                    AuthResponse res = await App.Sessions.LoginAsync(email, password);
                    if (res != null && !string.IsNullOrEmpty(res.token))
                    {
                        App.Auth.Set(res.token, res.user, rememberCredentials: true, email, password);
                        SceneFlow.GoMainMenu();
                        return;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[Boot] Auto-login failed: " + ex.Message);
                }
            }

            SceneFlow.GoLogin();
        }
    }
}
