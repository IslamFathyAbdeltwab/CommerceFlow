using CommerceFlow.Application.Auth.Commands.LoginUser;
using CommerceFlow.Application.Auth.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Users;
using Moq;

namespace CommerceFlow.Application.Tests.Auth;

public sealed class LoginUserCommandHandlerTests
{
    // The one message both failure modes share - this string is the whole
    // point of the anti-enumeration rule; the two tests below assert it.
    private const string InvalidCredentials = "Invalid email or password.";

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private LoginUserCommandHandler CreateHandler() =>
        new(_users.Object, _passwordHasher.Object, _tokenService.Object, _unitOfWork.Object);

    private static User SomeUser() => User.Create("ada@shop.de", "Ada Lovelace", "PBKDF2$hash");

    [Fact]
    public async Task Login_WithUnknownEmail_ThrowsUnauthorizedWithTheSharedMessage()
    {
        var handler = CreateHandler();
        _users
            .Setup(x => x.GetByEmailAsync("ada@shop.de", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new LoginUserCommand("ada@shop.de", "S3cretPass"), CancellationToken.None));

        // identical to the wrong-password answer: the endpoint reveals
        // nothing about which accounts exist
        Assert.Equal(InvalidCredentials, exception.Message);
        _tokenService.Verify(x => x.CreateToken(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsUnauthorizedWithTheSharedMessage()
    {
        var handler = CreateHandler();
        var user = SomeUser();
        _users
            .Setup(x => x.GetByEmailAsync("ada@shop.de", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher
            .Setup(x => x.Verify("WrongPass1", user.PasswordHash))
            .Returns(false);

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new LoginUserCommand("ada@shop.de", "WrongPass1"), CancellationToken.None));

        // byte-for-byte the same answer as an unknown email
        Assert.Equal(InvalidCredentials, exception.Message);
        Assert.Null(user.LastLoginAtUtc); // no sign-in stamp for a failed attempt
        _tokenService.Verify(x => x.CreateToken(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndStampsTheSignIn()
    {
        var handler = CreateHandler();
        var user = SomeUser();
        var expires = DateTimeOffset.UtcNow.AddMinutes(60);
        _users
            .Setup(x => x.GetByEmailAsync("ada@shop.de", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher
            .Setup(x => x.Verify("S3cretPass", user.PasswordHash))
            .Returns(true);
        _tokenService
            .Setup(x => x.CreateToken(user))
            .Returns(new TokenResult("signed.jwt.token", expires));

        var result = await handler.Handle(
            new LoginUserCommand("ada@shop.de", "S3cretPass"),
            CancellationToken.None);

        Assert.Equal("signed.jwt.token", result.AccessToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(expires, result.ExpiresAtUtc);
        Assert.Equal(user.Id, result.User.Id);
        Assert.Equal("ada@shop.de", result.User.Email);
        Assert.Equal("Ada Lovelace", result.User.FullName);
        Assert.Equal(User.Roles.Customer, result.User.Role);
        Assert.NotNull(user.LastLoginAtUtc); // the one state login mutates
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
