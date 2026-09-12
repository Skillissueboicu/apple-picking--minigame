using System.Collections.Generic;
using System.Threading.Tasks;

namespace FarmerQuest.Net
{
    /// <summary>
    /// HTTP API for auth and ESG game sessions
    /// </summary>
    public sealed class GameSessionApi
    {
        private const string Root = "esg/game-sessions";
        private readonly ApiClient _api;

        public GameSessionApi(ApiClient api) => _api = api;

        public Task<AuthResponse> LoginAsync(string email, string password) =>
            _api.PostAsync<AuthResponse>("auth/login", new { email, password });

        public Task<AuthResponse> RegisterAsync(string name, string email, string password) =>
            _api.PostAsync<AuthResponse>("auth/register", new { name, email, password });

        public Task<SessionInfo> CreateAsync(string gameKind = "farmerquest") =>
            _api.PostAsync<SessionInfo>(Root, new { mode = 1, gameKind, arGameId = (string)null });

        public Task<SessionInfo> JoinByCodeAsync(string code) =>
            _api.PostAsync<SessionInfo>($"{Root}/join", new { code });

        public Task<SessionInfo> GetByCodeAsync(string code) =>
            _api.GetAsync<SessionInfo>($"{Root}/{code}");

        public Task<SessionInfo> StartAsync(string sessionId) =>
            _api.PostAsync<SessionInfo>($"{Root}/{sessionId}/start", new { });

        public Task LeaveAsync(string sessionId) =>
            _api.PostAsync<object>($"{Root}/{sessionId}/leave", new { });

        public Task InviteByUsernameAsync(string sessionId, string username) =>
            _api.PostAsync<SessionInfo>($"{Root}/{sessionId}/invite", new { username });
    }

    // Player entry inside a session roster
    [System.Serializable]
    public class SessionPlayer
    {
        public string userId;
        public string name;
        public bool isHost;
        public bool isActiveDriver;
    }

    // Server session payload (code, host, status, players)
    [System.Serializable]
    public class SessionInfo
    {
        public string sessionId;
        public string code;
        public int mode;
        public string hostUserId;
        public string activeDriverUserId;
        public string status;
        public string gameKind;
        public string arGameId;
        public List<SessionPlayer> players = new();
    }
}
