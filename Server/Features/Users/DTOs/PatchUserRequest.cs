using System.ComponentModel.DataAnnotations;

namespace FarmerQuest.Server.Features.Users.DTOs;

// Partial update body for PATCH /users/{id}. Null fields are left unchanged
public sealed class PatchUserRequest
{
    [EmailAddress]
    public string? Email { get; set; }

    // Name (min. 2 characters)
    [MinLength(2)]
    public string? Name { get; set; }

    // Password (min. 6 characters)
    [MinLength(6, ErrorMessage = "Password skal være mindst 6 tegn")]
    public string? Password { get; set; }

    public string? Role { get; set; }

    // Team name (max 200 characters)
    [MaxLength(200)]
    public string? Team { get; set; }
}
