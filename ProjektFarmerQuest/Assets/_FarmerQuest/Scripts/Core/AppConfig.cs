namespace FarmerQuest.Core
{
    /// <summary>
    /// API base URL, loaded from OnlineNetworkSettings when present
    /// </summary>
    public static class AppConfig
    {
        public static string ApiBaseUrl { get; set; } = "http://localhost:5192";

        // Applies apiUrl from Resources/OnlineNetworkSettings if available
        public static void ApplyFromResources()
        {
            var settings = UnityEngine.Resources.Load<OnlineNetworkSettings>("OnlineNetworkSettings");
            if (settings != null && !string.IsNullOrWhiteSpace(settings.apiUrl))
                ApiBaseUrl = settings.apiUrl.TrimEnd('/');
        }
    }
}
