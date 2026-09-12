using System.ComponentModel.DataAnnotations;

namespace FarmerQuest.Server.Features.Users.DTOs;

// Body for PUT /users/{id}/role
public sealed class PutUserRoleRequest
{
    [Required]
    public string Role { get; set; } = string.Empty;
}
