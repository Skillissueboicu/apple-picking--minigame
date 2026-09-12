namespace FarmerQuest.Server.Features.Social.DTOs;

// A friend entry for list responses
public sealed record FriendResponse(string UserId, string Name);

// A friend request with both parties' ids, names and creation time
public sealed record FriendRequestResponse(
    long Id,
    string FromUserId,
    string FromName,
    string ToUserId,
    string ToName,
    DateTime CreatedAt);

// Body for sending a friend request (display name)
public sealed record SendFriendRequestBody(string Username);

// A blocked user entry for list responses
public sealed record BlockResponse(string UserId, string Name, DateTime CreatedAt);

// Body for blocking a user by display name
public sealed record BlockUserBody(string Username);
