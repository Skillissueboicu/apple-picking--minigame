using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using FarmerQuest.Server.Models;
using FarmerQuest.Server.Options;

namespace FarmerQuest.Server.Security;

/// <summary>
/// Centralizes JWT creation so services only depend on IJwtTokenFactory
/// </summary>
public sealed class JwtTokenFactory : IJwtTokenFactory
{
    private readonly JwtOptions _options;
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtTokenFactory(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    // Builds a signed JWT with id, email, name, and role claims
    public string CreateAccessToken(User user)
    {
        if (string.IsNullOrWhiteSpace(_options.Key))
            throw new InvalidOperationException("Jwt:Key er ikke sat i konfiguration.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Standard claim types used by [Authorize] and role checks
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(_options.ExpiryDays),
            signingCredentials: creds);

        return _handler.WriteToken(token);
    }
}
