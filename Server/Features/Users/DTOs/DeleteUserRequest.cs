namespace FarmerQuest.Server.Features.Users.DTOs;

// Optional body for DELETE /users/{id}
public sealed class DeleteUserRequest
{
    // Optional deletion reason (audit/logging)
    public string? Reason { get; set; }
}
