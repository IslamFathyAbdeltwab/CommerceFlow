using CommerceFlow.Application.Auth.Interfaces;

namespace CommerceFlow.API.Common;

// Reads the identity the JWT middleware already verified. Inbound claim
// mapping is off in Program.cs, so the raw claim names are stable and line
// up with what JwtTokenService writes: "sub" is the user id.
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId =>
        Guid.TryParse(
            httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value,
            out var id)
                ? id
                : Guid.Empty;

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
