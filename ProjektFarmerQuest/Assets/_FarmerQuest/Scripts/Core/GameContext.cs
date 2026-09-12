using FarmerQuest.Net;

namespace FarmerQuest.Core
{
    /// <summary>
    /// Whether the lobby should create a new session or join an existing one
    /// </summary>
    public enum LobbyIntent
    {
        Create,
        Join,
    }

    // Cross-scene state for lobby intent and the active network session
    public static class GameContext
    {
        public static LobbyIntent Intent;
        public static string PendingJoinCode;
        public static string SelectedGameKind = "farmerquest";
        public static INetworkSession Session;
        public static SessionInfo CurrentSession;
    }
}
