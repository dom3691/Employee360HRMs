using System.Security.Cryptography;
using Employee360.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Employee360.Infrastructure.Services;

/// <summary>
/// AES-256-CBC implementation of <see cref="IEncryptionService"/> (NFR-SEC-004).
/// A random IV is generated per value and prepended to the ciphertext, so equal
/// plaintexts produce different stored values. Key: base64-encoded 32 bytes from
/// "Encryption:Key" (Azure Key Vault in production).
/// </summary>
public sealed class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(IConfiguration configuration)
    {
        var keyBase64 = configuration["Encryption:Key"]
            ?? throw new InvalidOperationException("Missing 'Encryption:Key' configuration.");

        _key = Convert.FromBase64String(keyBase64);

        if (_key.Length != 32)
        {
            throw new InvalidOperationException(
                "'Encryption:Key' must be a base64-encoded 256-bit (32-byte) key.");
        }
    }

    /// <inheritdoc />
    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var payload = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, payload, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, payload, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(payload);
    }

    /// <inheritdoc />
    public string Decrypt(string cipherText)
    {
        var payload = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;

        var iv = new byte[aes.BlockSize / 8];
        Buffer.BlockCopy(payload, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(
            payload, iv.Length, payload.Length - iv.Length);

        return System.Text.Encoding.UTF8.GetString(plainBytes);
    }
}
