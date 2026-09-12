using Microsoft.EntityFrameworkCore;

using FarmerQuest.Server.Databases;
using FarmerQuest.Server.Features.Social.DTOs;
using FarmerQuest.Server.Models;

namespace FarmerQuest.Server.Features.Social;

/// <summary>
/// Friends, friend requests, and user blocks against the database
/// </summary>
public sealed class SocialService
{
    private readonly FarmerQuestDbContext _db;

    public SocialService(FarmerQuestDbContext db)
    {
        _db = db;
    }

    // Returns the caller's friends (other side of each friendship row)
    public async Task<List<FriendResponse>> GetFriendsAsync(string userId, CancellationToken ct)
    {
        List<Friendship> friendships = await _db.Friendships
            .AsNoTracking()
            .Where(f => f.UserIdA == userId || f.UserIdB == userId)
            .ToListAsync(ct);

        // Resolve the "other" user id from each undirected pair
        HashSet<string> friendIds = friendships
            .Select(f => f.UserIdA == userId ? f.UserIdB : f.UserIdA)
            .ToHashSet();

        if (friendIds.Count == 0)
            return [];

        return await _db.Users
            .AsNoTracking()
            .Where(u => friendIds.Contains(u.Id))
            .OrderBy(u => u.Name)
            .Select(u => new FriendResponse(u.Id, u.Name))
            .ToListAsync(ct);
    }

    // Pending requests addressed to the user (includes sender name)
    public async Task<List<FriendRequestResponse>> GetIncomingRequestsAsync(string userId, CancellationToken ct)
    {
        // Join sender for FromName; ToName left empty for incoming lists
        var rows = await (
            from r in _db.FriendRequests.AsNoTracking()
            join u in _db.Users.AsNoTracking() on r.FromUserId equals u.Id
            where r.ToUserId == userId && r.Status == "pending"
            orderby r.CreatedAt descending
            select new FriendRequestResponse(r.Id, r.FromUserId, u.Name, r.ToUserId, "", r.CreatedAt)
        ).ToListAsync(ct);

        return rows;
    }

    // Pending requests sent by the user (includes recipient name)
    public async Task<List<FriendRequestResponse>> GetOutgoingRequestsAsync(string userId, CancellationToken ct)
    {
        // Join recipient for ToName; FromName left empty for outgoing lists
        var rows = await (
            from r in _db.FriendRequests.AsNoTracking()
            join u in _db.Users.AsNoTracking() on r.ToUserId equals u.Id
            where r.FromUserId == userId && r.Status == "pending"
            orderby r.CreatedAt descending
            select new FriendRequestResponse(r.Id, r.FromUserId, "", r.ToUserId, u.Name, r.CreatedAt)
        ).ToListAsync(ct);

        return rows;
    }

    // Sends a pending friend request to the user with the given display name
    public async Task<(FriendRequestResponse? request, string? error)> SendFriendRequestAsync(
        string fromUserId, string username, CancellationToken ct)
    {
        string trimmed = username.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return (null, "Brugernavn mangler.");

        // Case-insensitive match on display name
        string lower = trimmed.ToLower();
        User? target = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Name.ToLower() == lower, ct);

        if (target == null)
            return (null, "Bruger ikke fundet.");
        if (target.Id == fromUserId)
            return (null, "Du kan ikke sende venneanmodning til dig selv.");

        if (await AreBlockedEitherWayAsync(fromUserId, target.Id, ct))
            return (null, "Du kan ikke sende venneanmodning til denne spiller.");

        if (await AreFriendsAsync(fromUserId, target.Id, ct))
            return (null, "I er allerede venner.");

        // Block duplicate pending in either direction
        bool pendingExists = await _db.FriendRequests.AnyAsync(r =>
            r.Status == "pending"
            && ((r.FromUserId == fromUserId && r.ToUserId == target.Id)
                || (r.FromUserId == target.Id && r.ToUserId == fromUserId)), ct);
        if (pendingExists)
            return (null, "Der er allerede en ventende venneanmodning.");

        User? fromUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == fromUserId, ct);
        string fromName = fromUser?.Name ?? "";

        var request = new FriendRequest
        {
            FromUserId = fromUserId,
            ToUserId = target.Id,
            Status = "pending",
        };
        _db.FriendRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        return (new FriendRequestResponse(
            request.Id, request.FromUserId, fromName, request.ToUserId, target.Name, request.CreatedAt), null);
    }

    // Accepts a pending request, creates a sorted friendship pair, and clears mirror pending
    public async Task<(bool ok, string? error)> AcceptFriendRequestAsync(
        long requestId, string userId, CancellationToken ct)
    {
        FriendRequest? request = await _db.FriendRequests
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);

        if (request == null)
            return (false, "Venneanmodning ikke fundet.");
        if (request.ToUserId != userId)
            return (false, "Venneanmodningen er ikke til dig.");
        if (request.Status != "pending")
            return (false, "Venneanmodningen er allerede besvaret.");

        if (await AreBlockedEitherWayAsync(request.FromUserId, request.ToUserId, ct))
            return (false, "Du kan ikke acceptere denne venneanmodning.");

        request.Status = "accepted";

        // Store friendship with sorted ids so lookups are unique either way
        if (!await AreFriendsAsync(request.FromUserId, request.ToUserId, ct))
        {
            (string a, string b) = SortedPair(request.FromUserId, request.ToUserId);
            _db.Friendships.Add(new Friendship
            {
                UserIdA = a,
                UserIdB = b,
            });
        }

        // Mark any reverse pending as accepted to clean up duplicates
        List<FriendRequest> mirrorPending = await _db.FriendRequests
            .Where(r => r.Id != request.Id
                        && r.Status == "pending"
                        && r.FromUserId == request.ToUserId
                        && r.ToUserId == request.FromUserId)
            .ToListAsync(ct);
        foreach (FriendRequest mirror in mirrorPending)
            mirror.Status = "accepted";

        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    // Declines a pending request addressed to the user
    public async Task<(bool ok, string? error)> DeclineFriendRequestAsync(
        long requestId, string userId, CancellationToken ct)
    {
        FriendRequest? request = await _db.FriendRequests
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);

        if (request == null)
            return (false, "Venneanmodning ikke fundet.");
        if (request.ToUserId != userId)
            return (false, "Venneanmodningen er ikke til dig.");
        // Already answered --> treat as success (idempotent)
        if (request.Status != "pending")
            return (true, null);

        request.Status = "declined";
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    // Removes the friendship between two users
    public async Task<(bool ok, string? error)> RemoveFriendAsync(
        string userId, string friendUserId, CancellationToken ct)
    {
        (string a, string b) = SortedPair(userId, friendUserId);
        Friendship? friendship = await _db.Friendships
            .FirstOrDefaultAsync(f => f.UserIdA == a && f.UserIdB == b, ct);

        if (friendship == null)
            return (false, "Venskab ikke fundet.");

        _db.Friendships.Remove(friendship);
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    // Lists users blocked by the caller
    public async Task<List<BlockResponse>> GetBlocksAsync(string userId, CancellationToken ct)
    {
        return await (
            from b in _db.UserBlocks.AsNoTracking()
            join u in _db.Users.AsNoTracking() on b.BlockedUserId equals u.Id
            where b.BlockerUserId == userId
            orderby b.CreatedAt descending
            select new BlockResponse(u.Id, u.Name, b.CreatedAt)
        ).ToListAsync(ct);
    }

    // Blocks a user by display name and removes friendship, pending requests, and related invites
    public async Task<(BlockResponse? block, string? error)> BlockUserAsync(
        string blockerUserId, string username, CancellationToken ct)
    {
        string trimmed = username.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return (null, "Brugernavn mangler.");

        string lower = trimmed.ToLower();
        User? target = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Name.ToLower() == lower, ct);

        if (target == null)
            return (null, "Bruger ikke fundet.");
        if (target.Id == blockerUserId)
            return (null, "Du kan ikke blokere dig selv.");

        bool alreadyBlocked = await _db.UserBlocks.AnyAsync(b =>
            b.BlockerUserId == blockerUserId && b.BlockedUserId == target.Id, ct);
        if (alreadyBlocked)
            return (null, "Spilleren er allerede blokeret.");

        var block = new UserBlock
        {
            BlockerUserId = blockerUserId,
            BlockedUserId = target.Id,
        };
        _db.UserBlocks.Add(block);

        // Remove existing friendship
        (string a, string b) = SortedPair(blockerUserId, target.Id);
        Friendship? friendship = await _db.Friendships
            .FirstOrDefaultAsync(f => f.UserIdA == a && f.UserIdB == b, ct);
        if (friendship != null)
            _db.Friendships.Remove(friendship);

        // Decline pending friend requests both ways
        List<FriendRequest> pendingRequests = await _db.FriendRequests
            .Where(r => r.Status == "pending"
                        && ((r.FromUserId == blockerUserId && r.ToUserId == target.Id)
                            || (r.FromUserId == target.Id && r.ToUserId == blockerUserId)))
            .ToListAsync(ct);
        foreach (FriendRequest req in pendingRequests)
            req.Status = "declined";

        // Decline pending game invites from the blocked user to the blocker
        List<GameSessionInvitation> pendingInvites = await _db.GameSessionInvitations
            .Where(i => i.Status == "pending"
                        && i.InvitedByUserId == target.Id
                        && i.InvitedUserId == blockerUserId)
            .ToListAsync(ct);
        foreach (GameSessionInvitation invite in pendingInvites)
            invite.Status = "declined";

        await _db.SaveChangesAsync(ct);

        return (new BlockResponse(target.Id, target.Name, block.CreatedAt), null);
    }

    // Removes a block row for the given pair
    public async Task<(bool ok, string? error)> UnblockUserAsync(
        string blockerUserId, string blockedUserId, CancellationToken ct)
    {
        UserBlock? block = await _db.UserBlocks
            .FirstOrDefaultAsync(b =>
                b.BlockerUserId == blockerUserId && b.BlockedUserId == blockedUserId, ct);

        if (block == null)
            return (false, "Blokering ikke fundet.");

        _db.UserBlocks.Remove(block);
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    // True if either user has blocked the other
    public Task<bool> AreBlockedEitherWayAsync(string userIdA, string userIdB, CancellationToken ct) =>
        _db.UserBlocks.AnyAsync(b =>
            (b.BlockerUserId == userIdA && b.BlockedUserId == userIdB)
            || (b.BlockerUserId == userIdB && b.BlockedUserId == userIdA), ct);


    // True if a friendship row exists for the sorted user-id pair
    private Task<bool> AreFriendsAsync(string userIdA, string userIdB, CancellationToken ct)
    {
        (string a, string b) = SortedPair(userIdA, userIdB);
        return _db.Friendships.AnyAsync(f => f.UserIdA == a && f.UserIdB == b, ct);
    }

    // Orders two user ids so friendship keys are direction-independent
    private static (string a, string b) SortedPair(string userId1, string userId2) =>
        string.CompareOrdinal(userId1, userId2) < 0
            ? (userId1, userId2)
            : (userId2, userId1);
}
