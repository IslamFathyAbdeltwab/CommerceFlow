using CommerceFlow.Application.Auth.Interfaces;
using CommerceFlow.Application.Auth.Queries.Dtos;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Users;
using MediatR;

namespace CommerceFlow.Application.Auth.Commands.LoginUser;

public sealed class LoginUserCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IUnitOfWork unitOfWork) : IRequestHandler<LoginUserCommand, LoginResultDto>
{
    // One message for both failure modes. "Unknown email" and "wrong
    // password" must be indistinguishable from outside, or the endpoint
    // becomes an account-enumeration oracle.
    private const string InvalidCredentials = "Invalid email or password.";

    public async Task<LoginResultDto> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetByEmailAsync(
            User.NormalizeEmail(request.Email),
            cancellationToken);

        // Pre-checks before anything moves: no last-login stamp, no token and
        // no save for a failed attempt.
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException(InvalidCredentials);

        user.RecordLogin();

        var token = tokenService.CreateToken(user);
        await unitOfWork.SaveChangesAsync(cancellationToken); // persists the sign-in stamp

        return new LoginResultDto(
            token.AccessToken,
            "Bearer",
            token.ExpiresAtUtc,
            new UserDto(user.Id, user.Email, user.FullName, user.Role));
    }
}
