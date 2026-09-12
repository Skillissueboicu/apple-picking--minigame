using System.ComponentModel.DataAnnotations;

namespace FarmerQuest.Server.Features.Users.DTOs;

// Full replace body for PUT /users/{id}
public sealed class PutUserRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    // Name (min. 2 characters)
    [Required, MinLength(2)]
    public string Name { get; set; } = string.Empty;

    // Password (min. 6 characters)
    [MinLength(6, ErrorMessage = "Password skal være mindst 6 tegn")]
    public string? Password { get; set; }

    public string? Role { get; set; }

    // Team name (max 200 characters)
    [MaxLength(200)]
    public string? Team { get; set; }
}
