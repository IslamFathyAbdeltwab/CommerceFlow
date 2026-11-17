using CommerceFlow.Application.Auth.Commands.RegisterUser;
using CommerceFlow.Application.Auth.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Users;
using Moq;

namespace CommerceFlow.Application.Tests.Auth;

public sealed class RegisterUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RegisterUserCommandHandler CreateHandler() =>
        new(_users.Object, _passwordHasher.Object, _unitOfWork.Object);

    [Fact]
    public async Task Register_WithNewEmail_CreatesHashedAccountAndSaves()
    {
        var handler = CreateHandler();
        var command = new RegisterUserCommand("ada@shop.de", "Ada Lovelace", "S3cretPass");
        _users
            .Setup(x => x.GetByEmailAsync("ada@shop.de", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordHasher
            .Setup(x => x.Hash("S3cretPass"))
            .Returns("PBKDF2$100000$salt$hash");

        var id = await handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        // what reaches the repository is the hash, never the plaintext
        _users.Verify(
            x => x.AddAsync(
                It.Is<User>(u =>
                    u.Id == id &&
                    u.Email == "ada@shop.de" &&
                    u.FullName == "Ada Lovelace" &&
                    u.PasswordHash == "PBKDF2$100000$salt$hash" &&
                    u.Role == User.Roles.Customer),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyTaken_ThrowsConflictWithoutTouchingAnything()
    {
        var handler = CreateHandler();
        _users
            .Setup(x => x.GetByEmailAsync("ada@shop.de", It.IsAny<CancellationToken>()))
            .ReturnsAsync(User.Create("ada@shop.de", "Existing Account", "PBKDF2$old"));

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new RegisterUserCommand("ada@shop.de", "Ada", "S3cretPass"),
                CancellationToken.None));

        Assert.Contains("already exists", exception.Message);
        _passwordHasher.Verify(x => x.Hash(It.IsAny<string>()), Times.Never);
        _users.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Register_WithCasedAndPaddedEmail_LooksUpAndStoresTheNormalizedForm()
    {
        var handler = CreateHandler();
        _users
            .Setup(x => x.GetByEmailAsync("ada@shop.de", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordHasher
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("PBKDF2$hash");

        await handler.Handle(
            new RegisterUserCommand("  Ada@Shop.DE  ", "  Ada  ", "S3cretPass"),
            CancellationToken.None);

        // one normalized form: what is looked up is what is stored, so the
        // unique index and the pre-check always agree
        _users.Verify(
            x => x.GetByEmailAsync("ada@shop.de", It.IsAny<CancellationToken>()),
            Times.Once);
        _users.Verify(
            x => x.AddAsync(
                It.Is<User>(u => u.Email == "ada@shop.de" && u.FullName == "Ada"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
