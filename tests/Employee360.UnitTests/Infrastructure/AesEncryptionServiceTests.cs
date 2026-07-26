using Employee360.Application.Common.Security;
using Employee360.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Employee360.UnitTests.Infrastructure;

/// <summary>Tests for AES-256 encryption at rest and masking (NFR-SEC-004).</summary>
public class AesEncryptionServiceTests
{
    internal static AesEncryptionService CreateService(string? keyBase64 = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = keyBase64
                    ?? Convert.ToBase64String(Enumerable.Repeat((byte)0x42, 32).ToArray()),
            })
            .Build();

        return new AesEncryptionService(configuration);
    }

    [Fact]
    public void EncryptDecrypt_ShouldRoundTrip()
    {
        var service = CreateService();

        var cipher = service.Encrypt("0123456789"); // NUBAN account number

        cipher.Should().NotBe("0123456789");
        service.Decrypt(cipher).Should().Be("0123456789");
    }

    [Fact]
    public void Encrypt_SameValueTwice_ShouldProduceDifferentCiphertexts()
    {
        var service = CreateService();

        var first = service.Encrypt("12345678901");
        var second = service.Encrypt("12345678901");

        first.Should().NotBe(second); // random IV per value
        service.Decrypt(first).Should().Be(service.Decrypt(second));
    }

    [Fact]
    public void Constructor_WithWrongKeyLength_ShouldThrow()
    {
        var act = () => CreateService(Convert.ToBase64String(new byte[16]));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*256-bit*");
    }

    [Fact]
    public void Constructor_WithMissingKey_ShouldThrow()
    {
        var configuration = new ConfigurationBuilder().Build();

        var act = () => new AesEncryptionService(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Encryption:Key*");
    }

    [Theory]
    [InlineData("0123456789", "******6789")]
    [InlineData("12345678901", "*******8901")]
    [InlineData("123", "***")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Masking_ShouldRevealOnlyTrailingDigits(string? value, string expected)
    {
        Masking.Mask(value).Should().Be(expected);
    }
}
