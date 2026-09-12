using System.Collections.Concurrent;

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

using FarmerQuest.Server.Databases;
using FarmerQuest.Server.GameFeatures.GameSessions;
using FarmerQuest.Server.Models;

namespace FarmerQuest.Server.GameFeatures.Farm;

/// <summary>
/// Server-authoritative farm session: commands are processed sequentially per session,
/// simulation runs only on the server, and all clients receive the same snapshot.
///
/// Persistence:
/// - Ticks update only the live GameSession (multiplayer sync).
/// - FarmSave is written on successful player actions and on leave.
/// </summary>
public sealed class FarmSessionAuthority : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<GameSessionHub> _hub;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly ILogger<FarmSessionAuthority> _logger;
    private Timer? _tickTimer;
    private int _tickInFlight;

    public FarmSessionAuthority(
        IServiceScopeFactory scopeFactory,
        IHubContext<GameSessionHub> hub,
        ILogger<FarmSessionAuthority> logger)
    {
        _scopeFactory = scopeFactory;
        _hub = hub;
        _logger = logger;
    }

    // Starts the simulation timer for all active farm sessions
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _tickTimer = new Timer(
            _ => _ = TickAllAsync(),
            null,
            TimeSpan.FromSeconds(FarmSimulation.TickInterval),
            TimeSpan.FromSeconds(FarmSimulation.TickInterval));
        return Task.CompletedTask;
    }

    // Stops the simulation timer
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _tickTimer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    // Disposes the tick timer
    public void Dispose() => _tickTimer?.Dispose();

    // Applies a player command under a per-session lock, persists, and broadcasts state
    public async Task<FarmCommandResult> ExecuteCommandAsync(
        string sessionId, string userId, string userName, FarmCommandRequest command, CancellationToken ct)
    {
        // Serialize all mutations for this session
        SemaphoreSlim gate = _locks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            FarmerQuestDbContext db = scope.ServiceProvider.GetRequiredService<FarmerQuestDbContext>();

            GameSession? session = await db.GameSessions
                .Include(s => s.Players)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId, ct);

            if (session == null)
                return Fail("Sessionen kan ikke findes.");
            if (session.Status != "playing")
            {
                string statusHint = session.Status switch
                {
                    "lobby" => "Spillet er ikke startet endnu - vent til host trykker Start.",
                    "ended" => "Sessionen er afsluttet.",
                    _ => $"Sessionen er ikke aktiv (status: {session.Status}).",
                };
                return Fail(statusHint);
            }
            if (session.GameKind != "farm")
                return Fail("Denne session er ikke en gård-session.");
            if (session.Players.All(p => p.UserId != userId))
                return Fail("Du er ikke med i denne session.");

            FarmSnapshotSerializer.FarmSessionSnapshot snapshot =
                FarmSnapshotSerializer.Parse(session.StateJson);
            FarmGameState farm = FarmSnapshotSerializer.LoadFarm(snapshot);

            (bool ok, string? error, string? action) =
                FarmCommandProcessor.Apply(farm, snapshot, command, userId);

            if (!ok)
            {
                return new FarmCommandResult
                {
                    Ok = false,
                    Error = error,
                    Version = session.StateVersion,
                    StateJson = session.StateJson,
                };
            }

            // Focus changes are presence-only; keep previous focus when not setFocus
            bool isFocus = command.Type == "setFocus";
            TouchPresence(snapshot, userId, userName, isFocus
                ? command.FocusedBuilding
                : snapshot.Players.TryGetValue(userId, out var p) ? p.FocusedBuilding : -1);

            // Actual gameplay actions increment the farm version and record who last changed it
            if (!isFocus)
            {
                snapshot.FarmVersion++;
                snapshot.LastFarmEditorId = userId;
                snapshot.LastAction = action;
            }

            FarmSnapshotSerializer.SaveFarm(snapshot, farm);
            session.StateJson = FarmSnapshotSerializer.Serialize(snapshot);
            session.StateVersion++;
            session.UpdatedAt = DateTime.UtcNow;

            // Durable save only for gameplay actions 
            if (!isFocus)
                await SyncFarmSaveAsync(db, session, ct);

            await db.SaveChangesAsync(ct);
            await BroadcastStateAsync(session.SessionId, session.StateJson, ct);

            return new FarmCommandResult
            {
                Ok = true,
                Error = null,
                AppliedAction = action,
                Version = session.StateVersion,
                StateJson = session.StateJson,
            };
        }
        finally
        {
            gate.Release();
        }
    }

    // Ticks every playing farm session; skips overlapping runs via a reentrancy flag
    private async Task TickAllAsync()
    {
        // Skip if a previous tick sweep is still running
        if (Interlocked.Exchange(ref _tickInFlight, 1) == 1)
            return;

        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            FarmerQuestDbContext db = scope.ServiceProvider.GetRequiredService<FarmerQuestDbContext>();

            List<string> sessionIds = await db.GameSessions
                .AsNoTracking()
                .Where(s => s.Status == "playing" && s.GameKind == "farm")
                .Select(s => s.SessionId)
                .ToListAsync();

            foreach (string sessionId in sessionIds)
            {
                try
                {
                    await TickSessionAsync(db, sessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Farm tick fejlede for session {SessionId}.", sessionId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Farm tick fejlede.");
        }
        finally
        {
            Interlocked.Exchange(ref _tickInFlight, 0);
        }
    }

    // Advances one session's simulation if the lock is free; does not touch FarmSave
    private async Task TickSessionAsync(FarmerQuestDbContext db, string sessionId)
    {
        SemaphoreSlim gate = _locks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
        // Don't block ticks behind a long player command - try lock or skip
        if (!await gate.WaitAsync(0))
            return;

        try
        {
            // Reload under lock so we don't overwrite newer command state
            GameSession? session = await db.GameSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
            if (session == null || session.Status != "playing" || session.GameKind != "farm")
                return;

            FarmSnapshotSerializer.FarmSessionSnapshot snapshot =
                FarmSnapshotSerializer.Parse(session.StateJson);
            FarmGameState farm = FarmSnapshotSerializer.LoadFarm(snapshot);

            FarmSimulation.Tick(farm, FarmSimulation.TickInterval);
            snapshot.FarmVersion++;

            FarmSnapshotSerializer.SaveFarm(snapshot, farm);
            session.StateJson = FarmSnapshotSerializer.Serialize(snapshot);
            session.StateVersion++;
            session.UpdatedAt = DateTime.UtcNow;

            // No FarmSave here - live session sync for clients only
            await db.SaveChangesAsync(CancellationToken.None);
            await BroadcastStateAsync(session.SessionId, session.StateJson, CancellationToken.None);
        }
        finally
        {
            gate.Release();
        }
    }

    // Copies the live session snapshot into the linked durable FarmSave row
    private static async Task SyncFarmSaveAsync(
        FarmerQuestDbContext db, GameSession session, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(session.FarmSaveId) || string.IsNullOrWhiteSpace(session.StateJson))
            return;

        FarmSave? save = await db.FarmSaves
            .FirstOrDefaultAsync(s => s.SaveId == session.FarmSaveId, ct);
        if (save != null)
            FarmSaveSync.ApplyFromSession(save, session);
    }

    // Upserts player presence (name + focused building) on the snapshot
    private static void TouchPresence(
        FarmSnapshotSerializer.FarmSessionSnapshot snapshot,
        string userId,
        string userName,
        int focusedBuilding)
    {
        snapshot.Players[userId] = FarmSnapshotSerializer.NewPresence(userId, userName, focusedBuilding);
    }

    // Pushes StateReceived to all SignalR clients in the session group
    private async Task BroadcastStateAsync(string sessionId, string? stateJson, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(stateJson)) return;
        await _hub.Clients.Group(sessionId).SendAsync("StateReceived", stateJson, ct);
    }

    // Builds a failed command result with an error message
    private static FarmCommandResult Fail(string error) =>
        new() { Ok = false, Error = error };
}
