using CommerceFlow.Application.Auth.Interfaces;
using CommerceFlow.Application.Auth.Queries.Dtos;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Domain.Entities.Users;
using MediatR;

namespace CommerceFlow.Application.Auth.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(
    ICurrentUser currentUser,
    IUserRepository users) : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        // The fallback policy authenticates before the action runs, so this
        // is the defensive path (empty/absent claims) - never a silent echo
        // of an anonymous caller.
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
            throw new UnauthorizedException("A valid access token is required.");

        // The token can outlive the account (deleted user, stale JWT): the
        // database, not the claim, decides whether the identity still exists.
        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken);

        if (user is null)
            throw new NotFoundException(nameof(User), currentUser.UserId);

        return new CurrentUserDto(
            user.Id,
            user.Email,
            user.FullName,
            user.Role,
            user.LastLoginAtUtc);
    }
}
