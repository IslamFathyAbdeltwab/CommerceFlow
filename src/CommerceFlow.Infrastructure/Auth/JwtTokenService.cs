using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CommerceFlow.Application.Auth.Interfaces;
using CommerceFlow.Domain.Entities.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CommerceFlow.Infrastructure.Auth;

// Signs a short-lived JWT with a symmetric key from configuration. The
// claim names are deliberately the raw ones ("sub", "email", "name",
// "role") on both sides of the wire: the API turns off inbound claim
// mapping, so what this writes is exactly what ICurrentUser reads back -
// no surprise renames in between.
public sealed class JwtTokenService(IConfiguration configuration) : ITokenService
{
    private const int DefaultExpirationMinutes = 60;

    public TokenResult CreateToken(User user)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = configuration["Jwt:Issuer"] ?? "CommerceFlow";
        var audience = configuration["Jwt:Audience"] ?? "CommerceFlow";
        var minutes = int.TryParse(configuration["Jwt:ExpirationMinutes"], out var configured)
            ? configured
            : DefaultExpirationMinutes;

        var expires = DateTimeOffset.UtcNow.AddMinutes(minutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.FullName),
            new Claim("role", user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
