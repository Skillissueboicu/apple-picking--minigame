using UnityEngine;

namespace FarmerQuest.Core
{
    /// <summary>
    /// DontDestroyOnLoad host for coroutines (e.g. session polling).
    /// Place on Boot.unity - never created at runtime.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class AppHost : MonoBehaviour
    {
        public static AppHost Instance { get; private set; }

        /// <summary>
        /// Finds the scene-placed AppHost. Does not create a GameObject.
        /// </summary>
        internal static AppHost EnsureHost()
        {
            if (Instance != null) return Instance;

            AppHost found = Object.FindFirstObjectByType<AppHost>(FindObjectsInactive.Include);
            if (found != null)
            {
                Instance = found;
                return Instance;
            }

            return null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
