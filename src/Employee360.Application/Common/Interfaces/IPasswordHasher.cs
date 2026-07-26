namespace Employee360.Application.Common.Interfaces;

/// <summary>
/// Password hashing abstraction (NFR-SEC-002: PBKDF2/bcrypt, never plain text).
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Hashes a plain-text password with a random salt.</summary>
    /// <param name="password">The plain-text password.</param>
    /// <returns>A self-describing hash string safe to persist.</returns>
    string Hash(string password);

    /// <summary>Verifies a plain-text password against a stored hash in constant time.</summary>
    /// <param name="password">The plain-text password to verify.</param>
    /// <param name="passwordHash">The stored hash.</param>
    bool Verify(string password, string passwordHash);
}
