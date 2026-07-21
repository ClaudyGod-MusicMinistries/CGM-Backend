using System.Security.Cryptography;
using System.Text;
using ClaudyGod.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ClaudyGod.Infrastructure.Services;

/// <summary>
/// Field-level encryption using AES-256-GCM (authenticated encryption). Unlike plain
/// AES-CBC, a tampered ciphertext fails to decrypt with a CryptographicException instead
/// of silently producing corrupted plaintext.
/// </summary>
public class EncryptionService : IEncryptionService
{
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private readonly byte[] _key;

    public EncryptionService(IConfiguration config)
    {
        var keyString = config["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption:Key is required.");
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(keyString));
    }

    public string Encrypt(string plainText)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSizeBytes];

        using (var aesGcm = new AesGcm(_key, TagSizeBytes))
        {
            aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
        }

        var result = new byte[nonce.Length + cipherBytes.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, nonce.Length, cipherBytes.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length + cipherBytes.Length, tag.Length);

        return Convert.ToBase64String(result);
    }

    /// <summary>Throws CryptographicException if the ciphertext was tampered with.</summary>
    public string Decrypt(string cipherText)
    {
        var data = Convert.FromBase64String(cipherText);

        if (data.Length < NonceSizeBytes + TagSizeBytes)
            throw new CryptographicException("Ciphertext is too short to be valid.");

        var nonce = new byte[NonceSizeBytes];
        var tag = new byte[TagSizeBytes];
        var cipherBytes = new byte[data.Length - NonceSizeBytes - TagSizeBytes];

        Buffer.BlockCopy(data, 0, nonce, 0, NonceSizeBytes);
        Buffer.BlockCopy(data, NonceSizeBytes, cipherBytes, 0, cipherBytes.Length);
        Buffer.BlockCopy(data, NonceSizeBytes + cipherBytes.Length, tag, 0, TagSizeBytes);

        var plainBytes = new byte[cipherBytes.Length];

        using (var aesGcm = new AesGcm(_key, TagSizeBytes))
        {
            aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
        }

        return Encoding.UTF8.GetString(plainBytes);
    }
}
