using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

using FarmerQuest.Server.Databases;
using FarmerQuest.Server.GameFeatures.GameSessions.DTOs;
using FarmerQuest.Server.GameFeatures.Farm;
using FarmerQuest.Server.GameFeatures.FarmSaves;
using FarmerQuest.Server.Models;

namespace FarmerQuest.Server.GameFeatures.GameSessions;

/// <summary>
/// Business logic for creating/joining sessions, invites, host transfer, and state sync.
/// Broadcasts lobby updates over SignalR.
/// </summary>
public sealed class GameSessionService
{
    // Alphabet without easily confused chars
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 6;

    private readonly FarmerQuestDbContext _db;
    private readonly IHubContext<GameSessionHub> _hub;

    public GameSessionService(FarmerQuestDbContext db, IHubContext<GameSessionHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    /// <summary>
    /// Creates a new session, or resumes an active farm session on the same save.
    /// For farm without FarmSaveId, creates a new owned save first.
    /// </summary>
    public async Task<(SessionResponse? session, string? error)> CreateAsync(
        string userId, string userName, CreateSessionRequest req, CancellationToken ct)
    {
        // Mode 0 = singleplayer, anything else treated as multiplayer (2)
        int mode = req.Mode == 0 ? 0 : 2;
        string? gameKind = string.IsNullOrWhiteSpace(req.GameKind) ? "farm" : req.GameKind;
        string? farmSaveId = string.IsNullOrWhiteSpace(req.FarmSaveId) ? null : req.FarmSaveId.Trim();

        if (gameKind == "farm")
        {
            if (farmSaveId == null)
            {
                // No save specified → create a fresh owned farm save
                (FarmSave? created, string? createError) = await CreateOwnedFarmSaveAsync(userId, userName, ct);
                if (created == null) return (null, createError);
                farmSaveId = created.SaveId;
            }
            else
            {
                FarmSave? owned = await _db.FarmSaves
                    .FirstOrDefaultAsync(s => s.SaveId == farmSaveId && s.OwnerUserId == userId, ct);
                if (owned == null)
                    return (null, "Save ikke fundet, eller du ejer det ikke.");

                // Resume active session on the same save (even if someone else is temporary host)
                GameSession? active = await _db.GameSessions
                    .Include(s => s.Players)
                    .FirstOrDefaultAsync(s =>
                        s.FarmSaveId == farmSaveId
                        && s.Status != "ended", ct);
                if (active != null)
                {
                    GameSessionPlayer? existing = active.Players.FirstOrDefault(p => p.UserId == userId);
                    if (existing == null)
                    {
                        existing = new GameSessionPlayer
                        {
                            SessionId = active.SessionId,
                            UserId = userId,
                            Name = userName,
                            IsHost = false,
                        };
                        active.Players.Add(existing);
                    }

                    // Save owner gets host back when they join/continue
                    TransferHost(active, userId);
                    active.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);

                    SessionResponse resumeDto = ToResponse(active);
                    await _hub.Clients.Group(active.SessionId).SendAsync("SessionUpdated", resumeDto, ct);
                    return (resumeDto, null);
                }
            }
        }

        // Brand-new session with caller as host
        var session = new GameSession
        {
            Code = await GenerateUniqueCodeAsync(ct),
            Mode = mode,
            GameKind = gameKind,
            FarmSaveId = farmSaveId,
            HostUserId = userId,
            Status = "lobby",
        };
        session.Players.Add(new GameSessionPlayer
        {
            SessionId = session.SessionId,
            UserId = userId,
            Name = userName,
            IsHost = true,
        });

        _db.GameSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        GameSession saved = await _db.GameSessions
            .Include(s => s.Players)
            .AsNoTracking()
            .FirstAsync(s => s.SessionId == session.SessionId, ct);

        SessionResponse dto = ToResponse(saved);
        await _hub.Clients.Group(saved.SessionId).SendAsync("SessionUpdated", dto, ct);
        return (dto, null);
    }

    // Creates a new FarmSave for the user if under the per-user cap
    private async Task<(FarmSave? save, string? error)> CreateOwnedFarmSaveAsync(
        string userId, string userName, CancellationToken ct)
    {
        int count = await _db.FarmSaves.CountAsync(s => s.OwnerUserId == userId, ct);
        if (count >= FarmSaveService.MaxSavesPerUser)
            return (null, $"Du kan højst have {FarmSaveService.MaxSavesPerUser} gemte gårde.");

        // Initial snapshot JSON + denormalized money/co2/debt for the save list
        FarmSnapshotSerializer.FarmSessionSnapshot snapshot =
            FarmSnapshotSerializer.CreateInitial(userId, userName);
        string json = FarmSnapshotSerializer.Serialize(snapshot);
        FarmGameState farm = FarmSnapshotSerializer.LoadFarm(snapshot);

        var save = new FarmSave
        {
            OwnerUserId = userId,
            DisplayName = $"Gård {count + 1}",
            StateJson = json,
            StateVersion = 1,
            Money = farm.Money,
            Co2 = farm.Co2,
            Debt = farm.Debt,
        };
        _db.FarmSaves.Add(save);
        await _db.SaveChangesAsync(ct);
        return (save, null);
    }

    // Joins by code; adds the player if not already present and marks invites accepted
    public async Task<(SessionResponse? session, string? error)> JoinByCodeAsync(
        string userId, string userName, string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            return (null, "Kode mangler.");

        GameSession? session = await _db.GameSessions
            .Include(s => s.Players)
            .FirstOrDefaultAsync(s => s.Code == code.ToUpperInvariant().Trim(), ct);

        if (session == null)
            return (null, "Ingen session med den kode.");
        if (session.Status == "ended")
            return (null, "Sessionen er afsluttet.");

        // Only add if not already a member; enforce max player count
        if (session.Players.All(p => p.UserId != userId))
        {
            if (!SessionStateMerge.CanJoin(session.Players.Count))
                return (null, "Sessionen er fuld (max 16 spillere).");

            session.Players.Add(new GameSessionPlayer
            {
                SessionId = session.SessionId,
                UserId = userId,
                Name = userName,
            });
            session.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        await MarkInvitationAcceptedAsync(session.SessionId, userId, ct);

        GameSession saved = await _db.GameSessions
            .Include(s => s.Players)
            .AsNoTracking()
            .FirstAsync(s => s.SessionId == session.SessionId, ct);

        SessionResponse dto = ToResponse(saved);
        await _hub.Clients.Group(saved.SessionId).SendAsync("SessionUpdated", dto, ct);
        return (dto, null);
    }

    // Returns a session by join code, or null if missing
    public async Task<SessionResponse?> GetByCodeAsync(string code, CancellationToken ct)
    {
        GameSession? session = await _db.GameSessions
            .Include(s => s.Players)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Code == code.ToUpperInvariant().Trim(), ct);
        return session == null ? null : ToResponse(session);
    }

    // Non-ended sessions where the user is a player, newest first
    public async Task<List<SessionResponse>> GetMineAsync(string userId, CancellationToken ct)
    {
        List<GameSession> sessions = await _db.GameSessions
            .Include(s => s.Players)
            .AsNoTracking()
            .Where(s => s.Status != "ended" && s.Players.Any(p => p.UserId == userId))
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(ct);
        return sessions.Select(ToResponse).ToList();
    }

    // Host starts the game. Idempotent if already playing
    public async Task<(SessionResponse? session, string? error)> StartAsync(
        string sessionId, string userId, CancellationToken ct)
    {
        GameSession? session = await _db.GameSessions
            .Include(s => s.Players)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, ct);

        if (session == null) return (null, "Session ikke fundet.");
        if (session.HostUserId != userId) return (null, "Kun vaerten kan starte spillet.");
        if (session.Status == "ended") return (null, "Sessionen er afsluttet.");

        // Already in progress (e.g. Continue/Load resume)
        if (session.Status == "playing")
            return (ToResponse(session), null);

        session.Status = "playing";
        session.UpdatedAt = DateTime.UtcNow;

        if (session.GameKind == "farm")
        {
            string hostName = session.Players.FirstOrDefault(p => p.UserId == userId)?.Name ?? "Spiller";
            await LoadFarmStateForStartAsync(session, userId, hostName, ct);
        }

        await _db.SaveChangesAsync(ct);

        SessionResponse dto = ToResponse(session);
        await _hub.Clients.Group(session.SessionId).SendAsync("GameStarted", dto, ct);
        return (dto, null);
    }

    /// <summary>
    /// Loads farm save JSON into the session for start, resetting presence to the host only.
    /// Falls back to a fresh initial snapshot if the save has no state
    /// </summary>
    private async Task LoadFarmStateForStartAsync(
        GameSession session, string userId, string hostName, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(session.FarmSaveId))
        {
            FarmSave? save = await _db.FarmSaves
                .FirstOrDefaultAsync(s => s.SaveId == session.FarmSaveId, ct);
            if (save != null && !string.IsNullOrWhiteSpace(save.StateJson))
            {
                FarmSnapshotSerializer.FarmSessionSnapshot snapshot =
                    FarmSnapshotSerializer.Parse(save.StateJson);
                // Presence resets on load — only the owner starts as present
                snapshot.Players = new Dictionary<string, FarmSnapshotSerializer.FarmPlayerPresenceState>
                {
                    [userId] = FarmSnapshotSerializer.NewPresence(userId, hostName, -1),
                };
                session.StateJson = FarmSnapshotSerializer.Serialize(snapshot);
                session.StateVersion = Math.Max(1, save.StateVersion);
                save.LastPlayedAt = DateTime.UtcNow;
                return;
            }
        }

        // No usable save state → start from a fresh farm snapshot
        session.StateJson = FarmSnapshotSerializer.Serialize(
            FarmSnapshotSerializer.CreateInitial(userId, hostName));
        session.StateVersion = 1;

        if (!string.IsNullOrEmpty(session.FarmSaveId))
        {
            FarmSave? save = await _db.FarmSaves
                .FirstOrDefaultAsync(s => s.SaveId == session.FarmSaveId, ct);
            if (save != null)
                FarmSaveSync.ApplyFromSession(save, session);
        }
    }

    // Host invites a user by username; upgrades singleplayer to multiplayer
    public async Task<(SessionResponse? session, string? error)> InviteByUsernameAsync(
        string sessionId, string requestingUserId, string username, CancellationToken ct)
    {
        GameSession? session = await _db.GameSessions
            .Include(s => s.Players)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, ct);

        if (session == null) return (null, "Session ikke fundet.");
        if (session.HostUserId != requestingUserId) return (null, "Kun vaerten kan invitere.");
        if (session.Status == "ended") return (null, "Sessionen er afsluttet.");

        string trimmedUsername = (username ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmedUsername))
            return (null, "Brugernavn mangler.");

        string lowerUsername = trimmedUsername.ToLower();
        User? target = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Name.ToLower() == lowerUsername, ct);
        if (target == null) return (null, "Bruger ikke fundet.");
        if (target.Id == requestingUserId) return (null, "Du kan ikke invitere dig selv.");

        // Block either direction blocks invites
        bool blocked = await _db.UserBlocks.AnyAsync(b =>
            (b.BlockerUserId == requestingUserId && b.BlockedUserId == target.Id)
            || (b.BlockerUserId == target.Id && b.BlockedUserId == requestingUserId), ct);
        if (blocked)
            return (null, "Du kan ikke invitere denne spiller.");

        if (session.Players.Any(p => p.UserId == target.Id))
            return (null, "Spilleren er allerede med i sessionen.");

        // Invite turns singleplayer into multiplayer.
        if (session.Mode == 0)
            session.Mode = 2;

        string hostName = session.Players.FirstOrDefault(p => p.UserId == requestingUserId)?.Name ?? "Vaert";
        GameSessionInvitation invitation = await UpsertPendingInvitationAsync(
            session, target.Id, requestingUserId, hostName, ct);

        SessionInvitationResponse inviteDto = ToInvitationResponse(invitation, session, hostName);
        await _hub.Clients.User(target.Id).SendAsync("Invited", inviteDto, ct);
        return (ToResponse(session), null);
    }

    // Pending invites for a user, excluding ended sessions
    public async Task<List<SessionInvitationResponse>> GetPendingInvitationsAsync(
        string userId, CancellationToken ct)
    {
        List<GameSessionInvitation> invites = await _db.GameSessionInvitations
            .Include(i => i.Session)!
            .ThenInclude(s => s!.Players)
            .AsNoTracking()
            .Where(i => i.InvitedUserId == userId
                        && i.Status == "pending"
                        && i.Session != null
                        && i.Session.Status != "ended")
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        return invites
            .Where(i => i.Session != null)
            .Select(i => ToInvitationResponse(i, i.Session!, HostName(i.Session!, i.InvitedByUserId)))
            .ToList();
    }

    // Accepts a pending invite by joining the session via its code
    public async Task<(SessionResponse? session, string? error)> AcceptInvitationAsync(
        long invitationId, string userId, string userName, CancellationToken ct)
    {
        GameSessionInvitation? invite = await _db.GameSessionInvitations
            .Include(i => i.Session)!
            .ThenInclude(s => s!.Players)
            .FirstOrDefaultAsync(i => i.InvitationId == invitationId, ct);

        if (invite == null) return (null, "Invitation ikke fundet.");
        if (invite.InvitedUserId != userId) return (null, "Invitationen er ikke til dig.");
        if (invite.Status != "pending") return (null, "Invitationen er allerede besvaret.");
        if (invite.Session == null || invite.Session.Status == "ended")
            return (null, "Sessionen er afsluttet.");

        (SessionResponse? session, string? error) joined = await JoinByCodeAsync(
            userId, userName, invite.Session.Code, ct);
        if (joined.session == null) return joined;

        invite.Status = "accepted";
        await _db.SaveChangesAsync(ct);
        return joined;
    }

    // Declines a pending invite (idempotent if already answered)
    public async Task<(bool ok, string? error)> DeclineInvitationAsync(
        long invitationId, string userId, CancellationToken ct)
    {
        GameSessionInvitation? invite = await _db.GameSessionInvitations
            .FirstOrDefaultAsync(i => i.InvitationId == invitationId, ct);

        if (invite == null) return (false, "Invitation ikke fundet.");
        if (invite.InvitedUserId != userId) return (false, "Invitationen er ikke til dig.");
        if (invite.Status != "pending") return (true, null);

        invite.Status = "declined";
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    // Creates or refreshes a pending invitation for the same session+user pair
    private async Task<GameSessionInvitation> UpsertPendingInvitationAsync(
        GameSession session,
        string invitedUserId,
        string invitedByUserId,
        string invitedByName,
        CancellationToken ct)
    {
        GameSessionInvitation? existing = await _db.GameSessionInvitations
            .FirstOrDefaultAsync(i =>
                i.SessionId == session.SessionId
                && i.InvitedUserId == invitedUserId
                && i.Status == "pending", ct);

        if (existing != null)
        {
            // Refresh timestamp/inviter so re-invites bump the invite to the top
            existing.CreatedAt = DateTime.UtcNow;
            existing.InvitedByUserId = invitedByUserId;
            existing.InvitedByName = invitedByName;
            await _db.SaveChangesAsync(ct);
            return existing;
        }

        var invitation = new GameSessionInvitation
        {
            SessionId = session.SessionId,
            InvitedUserId = invitedUserId,
            InvitedByUserId = invitedByUserId,
            InvitedByName = invitedByName,
            Status = "pending",
        };
        _db.GameSessionInvitations.Add(invitation);
        await _db.SaveChangesAsync(ct);
        return invitation;
    }

    // Marks all pending invites for this user on the session as accepted
    private async Task MarkInvitationAcceptedAsync(string sessionId, string userId, CancellationToken ct)
    {
        List<GameSessionInvitation> pending = await _db.GameSessionInvitations
            .Where(i => i.SessionId == sessionId && i.InvitedUserId == userId && i.Status == "pending")
            .ToListAsync(ct);
        foreach (GameSessionInvitation invite in pending)
            invite.Status = "accepted";
        if (pending.Count > 0)
            await _db.SaveChangesAsync(ct);
    }

    // Display name of a player in the session, or a default host label
    private static string HostName(GameSession session, string hostUserId) =>
        session.Players.FirstOrDefault(p => p.UserId == hostUserId)?.Name ?? "Vaert";

    // Maps invitation + session to the client DTO
    private static SessionInvitationResponse ToInvitationResponse(
        GameSessionInvitation invite, GameSession session, string hostName) =>
        new(
            invite.InvitationId,
            session.SessionId,
            session.Code,
            session.HostUserId,
            hostName,
            session.GameKind,
            invite.Status,
            new DateTimeOffset(invite.CreatedAt).ToUnixTimeMilliseconds());

    // Removes the player; flushes farm state to FarmSave; ends session or transfers host
    public async Task<(bool ok, string? error)> LeaveAsync(
        string sessionId, string userId, CancellationToken ct)
    {
        GameSession? session = await _db.GameSessions
            .Include(s => s.Players)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, ct);
        if (session == null) return (false, "Session ikke fundet.");

        bool wasHost = session.HostUserId == userId;

        GameSessionPlayer? player = session.Players.FirstOrDefault(p => p.UserId == userId);
        if (player != null)
        {
            session.Players.Remove(player);
            _db.GameSessionPlayers.Remove(player);
        }

        // Flush to FarmSave (owner) - even if the session continues with a new host
        if (session.GameKind == "farm"
            && !string.IsNullOrEmpty(session.FarmSaveId)
            && !string.IsNullOrWhiteSpace(session.StateJson))
        {
            FarmSave? save = await _db.FarmSaves
                .FirstOrDefaultAsync(s => s.SaveId == session.FarmSaveId, ct);
            if (save != null)
                FarmSaveSync.ApplyFromSession(save, session);
        }

        if (session.Players.Count == 0)
        {
            session.Status = "ended";
        }
        else if (wasHost)
        {
            // Session handoff: next player becomes host. FarmSave ownership unchanged
            GameSessionPlayer nextHost = session.Players
                .OrderBy(p => p.JoinedAt)
                .First();
            TransferHost(session, nextHost.UserId);
        }

        session.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _hub.Clients.Group(session.SessionId).SendAsync("SessionUpdated", ToResponse(session), ct);
        return (true, null);
    }

    // Sets temporary session host. Does not touch FarmSave.OwnerUserId
    private static void TransferHost(GameSession session, string newHostUserId)
    {
        session.HostUserId = newHostUserId;
        foreach (GameSessionPlayer p in session.Players)
            p.IsHost = p.UserId == newHostUserId;
    }

    /// <summary>
    /// Save and broadcast game state. All players in multiplayer may submit.
    /// Farm sessions use /farm/command instead of client-pushed state
    /// </summary>
    public async Task<(SessionStateResponse? state, string? error)> SubmitStateAsync(
        string sessionId, string userId, string userName, string stateJson, CancellationToken ct)
    {
        GameSession? session = await _db.GameSessions
            .Include(s => s.Players)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, ct);
        if (session == null)
            return (null, "Sessionen findes ikke (forkert eller udløbet session-id).");
        if (session.Status != "playing")
        {
            string statusHint = session.Status switch
            {
                "lobby" => "Spillet er ikke startet endnu — vent til værten trykker Start.",
                "ended" => "Sessionen er afsluttet.",
                _ => $"Sessionen er ikke aktiv (status: {session.Status}).",
            };
            return (null, statusHint);
        }
        if (session.Players.All(p => p.UserId != userId))
            return (null, "Du er ikke med i denne session.");

        // Farm uses commands + server simulation — clients must not push farm state
        if (session.GameKind == "farm")
            return (null, "Brug /farm/command til gård-sessioner.");

        session.StateJson = stateJson;
        session.StateVersion++;
        session.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _hub.Clients.Group(session.SessionId).SendAsync("StateReceived", session.StateJson, ct);
        return (new SessionStateResponse(session.StateVersion, session.StateJson), null);
    }

    // Returns version + StateJson for polling clients
    public async Task<SessionStateResponse?> GetStateAsync(string sessionId, CancellationToken ct)
    {
        GameSession? session = await _db.GameSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, ct);
        return session == null ? null : new SessionStateResponse(session.StateVersion, session.StateJson);
    }

    // Generates a unique short join code, with a longer GUID fallback
    private async Task<string> GenerateUniqueCodeAsync(CancellationToken ct)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            string code = GenerateCode();
            bool exists = await _db.GameSessions.AnyAsync(s => s.Code == code, ct);
            if (!exists) return code;
        }
        // Fall back to a longer, almost-always-unique id.
        return Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
    }

    // Random CodeLength string from CodeAlphabet
    private static string GenerateCode()
    {
        Span<char> chars = stackalloc char[CodeLength];
        for (int i = 0; i < CodeLength; i++)
            chars[i] = CodeAlphabet[Random.Shared.Next(CodeAlphabet.Length)];
        return new string(chars);
    }

    // Maps a GameSession entity to the API response DTO
    private static SessionResponse ToResponse(GameSession s) =>
        new(
            s.SessionId,
            s.Code,
            s.Mode,
            s.HostUserId,
            s.ActiveDriverUserId,
            s.Status,
            s.GameKind,
            s.FarmSaveId,
            s.Players
                .OrderByDescending(p => p.IsHost)
                .ThenBy(p => p.JoinedAt)
                .Select(p => new SessionPlayerResponse(
                    p.UserId,
                    p.Name,
                    p.IsHost,
                    false))
                .ToList());
}
