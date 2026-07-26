namespace Employee360.Application.Common.Interfaces;

/// <summary>
/// Symmetric encryption for sensitive PII at rest — bank account numbers and NIN
/// (NFR-SEC-004: AES-256). Implemented in Infrastructure with a Key Vault-sourced key.
/// </summary>
public interface IEncryptionService
{
    /// <summary>Encrypts plain text, returning a self-contained cipher string.</summary>
    /// <param name="plainText">The value to encrypt.</param>
    string Encrypt(string plainText);

    /// <summary>Decrypts a cipher string produced by <see cref="Encrypt"/>.</summary>
    /// <param name="cipherText">The encrypted value.</param>
    string Decrypt(string cipherText);
}
