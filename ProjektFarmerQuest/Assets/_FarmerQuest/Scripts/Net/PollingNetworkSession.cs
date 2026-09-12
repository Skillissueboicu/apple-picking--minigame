using System;
using System.Threading.Tasks;
using FarmerQuest.Core;
using UnityEngine;

namespace FarmerQuest.Net
{
    /// <summary>
    /// Simple polling session (no SignalR)
    /// </summary>
    public sealed class PollingNetworkSession : INetworkSession
    {
        public event Action<SessionInfo> OnSessionUpdated;
        public event Action<string> OnGameStarted;
        public event Action<string> OnError;

        public SessionInfo Current { get; private set; }

        private readonly GameSessionApi _api;
        private bool _running;
        private string _lastStatus;
        private bool _pollInFlight;

        public PollingNetworkSession(GameSessionApi api) => _api = api;

        // Marks the session as running; token is unused for HTTP polling
        public Task ConnectAsync(string token)
        {
            _running = true;
            return Task.CompletedTask;
        }

        // Creates a new session and starts polling for updates
        public async Task<SessionInfo> CreateAsync()
        {
            Current = await _api.CreateAsync(GameContext.SelectedGameKind ?? "farmerquest");
            _lastStatus = Current?.status;
            StartPolling();
            return Current;
        }

        // Joins an existing session by code and starts polling
        public async Task<SessionInfo> JoinByCodeAsync(string code)
        {
            Current = await _api.JoinByCodeAsync(code);
            _lastStatus = Current?.status;
            StartPolling();
            return Current;
        }

        public Task InviteByUsernameAsync(string username) =>
            Current == null
                ? Task.CompletedTask
                : _api.InviteByUsernameAsync(Current.sessionId, username);

        // Asks the host API to start the game and raises OnGameStarted
        public async Task StartAsync()
        {
            if (Current == null) return;
            Current = await _api.StartAsync(Current.sessionId);
            OnGameStarted?.Invoke(Current.sessionId);
        }

        // Stops polling and leaves the remote session when possible
        public async Task LeaveAsync()
        {
            _running = false;
            if (Current != null)
            {
                try { await _api.LeaveAsync(Current.sessionId); }
                catch { /* ignore leave errors */ }
            }
            Current = null;
        }

        public Task DisconnectAsync()
        {
            _running = false;
            return Task.CompletedTask;
        }

        private void StartPolling()
        {
            AppHost host = AppHost.EnsureHost();
            if (host == null)
            {
                Debug.LogError("[Polling] No AppHost - start Play from Boot so AppHost can persist.");
                return;
            }

            host.StartCoroutine(PollLoop());
        }

        private System.Collections.IEnumerator PollLoop()
        {
            // Reuse one WaitForSeconds to avoid per-tick allocations
            var wait = new WaitForSeconds(1.5f);
            while (_running && Current != null)
            {
                yield return wait;
                if (_pollInFlight) continue;
                _ = PollOnceAsync();
            }
        }

        private async Task PollOnceAsync()
        {
            if (Current == null || string.IsNullOrEmpty(Current.code)) return;

            _pollInFlight = true;
            try
            {
                SessionInfo latest = await _api.GetByCodeAsync(Current.code);
                if (latest == null || !_running) return;
                Current = latest;
                OnSessionUpdated?.Invoke(latest);

                // Detect transition into a running/playing state
                if (_lastStatus != "running" &&
                    (latest.status == "running" || latest.status == "playing"))
                {
                    _lastStatus = latest.status;
                    OnGameStarted?.Invoke(latest.sessionId);
                }
                else
                {
                    _lastStatus = latest.status;
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
            finally
            {
                _pollInFlight = false;
            }
        }
    }
}
