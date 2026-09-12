using UnityEngine;

namespace FarmerQuest.Core
{
    /// <summary>
    /// ScriptableObject for API base URL and remember-login duration
    /// </summary>
    [CreateAssetMenu(fileName = "OnlineNetworkSettings", menuName = "FarmerQuest/Network Settings")]
    public sealed class OnlineNetworkSettings : ScriptableObject
    {
        public string apiUrl = "http://localhost:5192";

        [Tooltip("How many days remembered login lasts (token + credentials).")]
        [Range(1, 30)]
        public int rememberDays = 7;
    }
}
