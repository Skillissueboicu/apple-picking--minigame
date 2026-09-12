using System.ComponentModel.DataAnnotations;

namespace FarmerQuest.Server.Features.Auth.DTOs;

// Body for POST /auth/login
public sealed class PostLoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
