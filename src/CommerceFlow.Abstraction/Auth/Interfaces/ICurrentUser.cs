namespace CommerceFlow.Application.Auth.Interfaces;

// The caller as the API sees them after the JWT middleware verified the
// token: claims resolved once at the boundary, so handlers never touch
// HttpContext. Implemented in the API composition root.
public interface ICurrentUser
{
    Guid UserId { get; }

    bool IsAuthenticated { get; }
}
