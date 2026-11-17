using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Users;

// Aggregate root: an account that can authenticate. The email is the login
// key, normalized exactly once - here, at the domain boundary - so
// "Ada@Shop.DE" and "ada@shop.de" can never become two accounts, and every
// lookup elsewhere can compare stored values directly.
//
// The password never reaches this class in plaintext: Create receives the
// hash produced behind the IPasswordHasher port (PBKDF2 in Infrastructure).
// The domain only guarantees that a hash exists and is kept in sync when the
// user signs in - how a hash is computed is deliberately not its business.
public class User : AggregateRoot
{
    public const int EmailMaxLength = 254;
    public const int FullNameMaxLength = 200;
    public const int PasswordHashMaxLength = 500;
    public const int RoleMaxLength = 50;

    // Roles ship as claims in the token. Authorization decisions (policies,
    // [Authorize(Roles = ...)]) are the natural next layer on top of this.
    public static class Roles
    {
        public const string Customer = "Customer";
        public const string Admin = "Admin";
    }

    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = Roles.Customer;

    // Stamped by the login handler after the password verified - it is a
    // record of a successful authentication, not of any single request.
    public DateTimeOffset? LastLoginAtUtc { get; private set; }

    private User() { } // required by EF Core

    public static User Create(string email, string fullName, string passwordHash)
    {
        var normalizedEmail = NormalizeEmail(email);

        if (normalizedEmail.Length == 0)
            throw new DomainException("Email is required.");

        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Full name is required.");

        // The hash - never the password - must accompany the account.
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash is required.");

        return new User
        {
            Email = normalizedEmail,
            FullName = fullName.Trim(),
            PasswordHash = passwordHash,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    // Single source of truth for the login key's shape. Used by Create and by
    // every handler that looks an account up, so storage and lookup always
    // agree on what "the same email" means.
    public static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();

    public void RecordLogin()
    {
        LastLoginAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
