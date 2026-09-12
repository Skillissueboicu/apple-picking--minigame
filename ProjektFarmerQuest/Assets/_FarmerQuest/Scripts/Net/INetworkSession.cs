using System;
using System.Threading.Tasks;

namespace FarmerQuest.Net
{
    /// <summary>
    /// Abstraction over create/join/start/leave for an online game session
    /// </summary>
    public interface INetworkSession
    {
        event Action<SessionInfo> OnSessionUpdated;
        event Action<string> OnGameStarted;
        event Action<string> OnError;

        SessionInfo Current { get; }

        Task ConnectAsync(string token);
        Task<SessionInfo> CreateAsync();
        Task<SessionInfo> JoinByCodeAsync(string code);
        Task InviteByUsernameAsync(string username);
        Task StartAsync();
        Task LeaveAsync();
        Task DisconnectAsync();
    }
}
