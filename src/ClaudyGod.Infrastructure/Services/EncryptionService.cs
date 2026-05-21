using System.Security.Cryptography;
using System.Text;
using ClaudyGod.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ClaudyGod.Infrastructure.Services;

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public EncryptionService(IConfiguration config)
    {
        var keyString = config["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption:Key is required.");
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(keyString));
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[aes.IV.Length + cipherBytes.Length];
        aes.IV.CopyTo(result, 0);
        cipherBytes.CopyTo(result, aes.IV.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        var data = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;

        var iv = new byte[aes.BlockSize / 8];
        var cipher = new byte[data.Length - iv.Length];
        Array.Copy(data, 0, iv, 0, iv.Length);
        Array.Copy(data, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }

}
