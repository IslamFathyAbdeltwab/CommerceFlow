using CommerceFlow.Domain.Entities.Users;

namespace CommerceFlow.Application.Auth.Interfaces;

// What an issued credential looks like to the rest of the application: the
// token itself plus when it stops working. JWT specifics (claims, signing
// algorithm, key) stay behind this port in Infrastructure.
public sealed record TokenResult(string AccessToken, DateTimeOffset ExpiresAtUtc);

public interface ITokenService
{
    TokenResult CreateToken(User user);
}
