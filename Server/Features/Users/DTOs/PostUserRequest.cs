using System.ComponentModel.DataAnnotations;


namespace FarmerQuest.Server.Features.Users.DTOs;

// Body for creating a user (admin/service path)
public sealed class PostUserRequest
{
    [Required, EmailAddress(ErrorMessage = "Ugyldig mailadresse")]
    public string Email { get; set; } = string.Empty;

    // Name (min. 2 characters)
    [Required, MinLength(2)]
    public string Name { get; set; } = string.Empty;
}
