using FarmerQuest.Net;
using FarmerQuest.UI;
using UnityEngine;

namespace FarmerQuest.Core
{
    /// <summary>
    /// App entry: auth, API client, and session API.
    /// </summary>
    public static class App
    {
        public static AuthSession Auth { get; private set; }
        public static ApiClient Api { get; private set; }
        public static PlayerStatsApi Stats { get; private set; }

        public static GameSessionApi Sessions { get; private set; }
        public static bool Initialized { get; private set; }

        // Services only - AppHost registers itself in Awake on Boot
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            if (Initialized) return;

            AppConfig.ApplyFromResources();

            Auth = new AuthSession();
            var settings = Resources.Load<OnlineNetworkSettings>("OnlineNetworkSettings");
            if (settings != null) Auth.RememberDays = settings.rememberDays;

            Auth.LoadFromPrefs();
            Api = new ApiClient(Auth);
            Sessions = new GameSessionApi(Api);
            Stats = new PlayerStatsApi(Api);


            Initialized = true;
            Debug.Log($"[FarmQuestOnline] API={AppConfig.ApiBaseUrl} auth={(Auth.IsAuthenticated ? "yes" : "no")}");
        }
    }
}
