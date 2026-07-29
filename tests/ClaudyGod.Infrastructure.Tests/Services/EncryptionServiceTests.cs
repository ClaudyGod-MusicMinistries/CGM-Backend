using System.Security.Cryptography;
using ClaudyGod.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace ClaudyGod.Infrastructure.Tests.Services;

public class EncryptionServiceTests
{
    private static EncryptionService NewService(string key = "test-encryption-key-32-chars!!!!") =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Encryption:Key"] = key })
            .Build());

    [Fact]
    public void Encrypt_Decrypt_RoundTripsToOriginalPlaintext()
    {
        var service = NewService();
        const string plaintext = "sensitive donor data: 4111-1111-1111-1111";

        var cipher = service.Encrypt(plaintext);
        var decrypted = service.Decrypt(cipher);

        decrypted.Should().Be(plaintext);
    }

    [Fact]
    public void Encrypt_SamePlaintextTwice_ProducesDifferentCiphertexts()
    {
        var service = NewService();
        const string plaintext = "same input";

        var cipher1 = service.Encrypt(plaintext);
        var cipher2 = service.Encrypt(plaintext);

        cipher1.Should().NotBe(cipher2); // fresh nonce each time
    }

    [Fact]
    public void Decrypt_TamperedCiphertext_ThrowsInsteadOfReturningCorruptedPlaintext()
    {
        var service = NewService();
        var cipherBytes = Convert.FromBase64String(service.Encrypt("original value"));

        // Flip a byte in the ciphertext portion (after the 12-byte nonce).
        cipherBytes[15] ^= 0xFF;
        var tampered = Convert.ToBase64String(cipherBytes);

        var act = () => service.Decrypt(tampered);

        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Decrypt_TamperedAuthTag_Throws()
    {
        var service = NewService();
        var cipherBytes = Convert.FromBase64String(service.Encrypt("original value"));

        // Flip the last byte, which falls within the 16-byte auth tag.
        cipherBytes[^1] ^= 0xFF;
        var tampered = Convert.ToBase64String(cipherBytes);

        var act = () => service.Decrypt(tampered);

        act.Should().Throw<CryptographicException>();
    }
}
