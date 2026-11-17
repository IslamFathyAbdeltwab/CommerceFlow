namespace CommerceFlow.Application.Auth.Interfaces;

// One-way password boundary: Application sends plaintext out once, at
// registration and at login, and only ever receives hashes back. The
// algorithm (PBKDF2 + salt today) lives in Infrastructure and can be rotated
// without touching a single handler.
public interface IPasswordHasher
{
    string Hash(string password);

    // Constant-time comparison is the implementation's job - callers just
    // ask "does this password match this hash?".
    bool Verify(string password, string passwordHash);
}
