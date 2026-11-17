using CommerceFlow.Domain.Entities.Users;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Tests.Users;

public sealed class UserTests
{
    [Fact]
    public void Create_TrimsAndLowercasesTheEmailOnce()
    {
        var user = User.Create("  Ada@Shop.DE  ", "Ada Lovelace", "PBKDF2$hash");

        // one normalized form is the account's identity: case and padding
        // differences can never split it in two
        Assert.Equal("ada@shop.de", user.Email);
    }

    [Fact]
    public void Create_TrimsTheFullName()
    {
        var user = User.Create("ada@shop.de", "   Ada Lovelace   ", "PBKDF2$hash");

        Assert.Equal("Ada Lovelace", user.FullName);
    }

    [Fact]
    public void Create_WithoutEmail_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            User.Create("   ", "Ada", "PBKDF2$hash"));

        Assert.Equal("Email is required.", exception.Message);
    }

    [Fact]
    public void Create_WithoutFullName_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            User.Create("ada@shop.de", "  ", "PBKDF2$hash"));

        Assert.Equal("Full name is required.", exception.Message);
    }

    [Fact]
    public void Create_WithoutAPasswordHash_ThrowsDomainException()
    {
        // the account must carry a hash - the plaintext password itself
        // never reaches the domain
        var exception = Assert.Throws<DomainException>(() =>
            User.Create("ada@shop.de", "Ada", ""));

        Assert.Equal("Password hash is required.", exception.Message);
    }

    [Fact]
    public void Create_DefaultsToTheCustomerRoleWithTimestamps()
    {
        var before = DateTimeOffset.UtcNow;
        var user = User.Create("ada@shop.de", "Ada", "PBKDF2$hash");

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(User.Roles.Customer, user.Role);
        Assert.Null(user.LastLoginAtUtc);
        Assert.InRange(user.CreatedAtUtc, before.AddSeconds(-5), DateTimeOffset.UtcNow.AddSeconds(5));
    }

    [Fact]
    public void RecordLogin_StampsTheSignInAndRefreshesTheAuditTime()
    {
        var user = User.Create("ada@shop.de", "Ada", "PBKDF2$hash");
        var before = DateTimeOffset.UtcNow;

        user.RecordLogin();

        Assert.NotNull(user.LastLoginAtUtc);
        Assert.InRange(user.LastLoginAtUtc!.Value, before.AddSeconds(-5), DateTimeOffset.UtcNow.AddSeconds(5));
        Assert.True(user.UpdatedAtUtc >= user.CreatedAtUtc);
    }

    [Fact]
    public void NormalizeEmail_IsIdempotent()
    {
        // registering and logging in both normalize before lookup; running
        // it twice must not change the answer
        var once = User.NormalizeEmail(" Ada@Shop.DE ");
        var twice = User.NormalizeEmail(once);

        Assert.Equal("ada@shop.de", once);
        Assert.Equal(once, twice);
    }
}
