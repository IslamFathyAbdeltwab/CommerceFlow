using System.Security.Cryptography;
using CommerceFlow.Application.Auth.Interfaces;

namespace CommerceFlow.Infrastructure.Auth;

// PBKDF2 (SHA-256, 100k iterations, 16-byte random salt) - the standard,
// built-in KDF, no hand-rolled crypto and no extra package. The stored
// format carries its own parameters ("PBKDF2$iterations$salt$hash"), so a
// future iteration bump can still verify old hashes and re-hash on the next
// successful login instead of invalidating every account.
public sealed class PasswordHasher : IPasswordHasher
{
    private const string Prefix = "PBKDF2";
    private const int Iterations = 100_000;
    private const int SaltSize = 16;   // 128 bits of randomness per account
    private const int HashSize = 32;   // 256-bit derived key

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        return string.Join('$',
            Prefix,
            Iterations.ToString(),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split('$');
        if (parts.Length != 4 || parts[0] != Prefix)
            return false;

        if (!int.TryParse(parts[1], out var iterations))
            return false;

        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false; // corrupt hash: never throw on a bad password check
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

        // Fixed-time: a timing side channel must not reveal how many leading
        // bytes of the hash a guess got right.
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
