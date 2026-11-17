using CommerceFlow.Application.Auth.Interfaces;
using CommerceFlow.Application.Auth.Queries.GetCurrentUser;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Domain.Entities.Users;
using Moq;

namespace CommerceFlow.Application.Tests.Auth;

public sealed class GetCurrentUserQueryHandlerTests
{
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IUserRepository> _users = new();

    private GetCurrentUserQueryHandler CreateHandler() =>
        new(_currentUser.Object, _users.Object);

    [Fact]
    public async Task GetCurrentUser_WithoutAnAuthenticatedCaller_ThrowsUnauthorized()
    {
        var handler = CreateHandler();
        _currentUser.SetupGet(x => x.IsAuthenticated).Returns(false);
        _currentUser.SetupGet(x => x.UserId).Returns(Guid.Empty);

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new GetCurrentUserQuery(), CancellationToken.None));

        Assert.Equal("A valid access token is required.", exception.Message);
        // never echoes an anonymous caller from nowhere
        _users.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetCurrentUser_WhenTokenOutlivesTheAccount_ThrowsNotFound()
    {
        var handler = CreateHandler();
        var userId = Guid.NewGuid();
        _currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        _currentUser.SetupGet(x => x.UserId).Returns(userId);
        _users
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetCurrentUserQuery(), CancellationToken.None));

        // the database, not the claim, has the last word on existence
        Assert.Contains("User with id", exception.Message);
        Assert.Contains(userId.ToString(), exception.Message);
    }

    [Fact]
    public async Task GetCurrentUser_WithKnownAccount_ReturnsTheClaimIdentityFromTheDatabase()
    {
        var handler = CreateHandler();
        var user = User.Create("ada@shop.de", "Ada Lovelace", "PBKDF2$hash");
        _currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        _currentUser.SetupGet(x => x.UserId).Returns(user.Id);
        _users
            .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        Assert.Equal(user.Id, result.Id);
        Assert.Equal("ada@shop.de", result.Email);
        Assert.Equal("Ada Lovelace", result.FullName);
        Assert.Equal(User.Roles.Customer, result.Role);
        Assert.Null(result.LastLoginAtUtc); // never signed in yet
    }
}
