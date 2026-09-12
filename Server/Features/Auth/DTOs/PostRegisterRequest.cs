using System.ComponentModel.DataAnnotations;

namespace FarmerQuest.Server.Features.Auth.DTOs;

// Body for POST /auth/register
public sealed class PostRegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    // Name (min. 2 characters)
    [Required, MinLength(2)]
    public string Name { get; set; } = string.Empty;

    // Password (min. 6 characters)
    [Required, MinLength(6, ErrorMessage = "Password skal være mindst 6 tegn")]
    public string Password { get; set; } = string.Empty;
}
