using Employee360.Infrastructure.Identity;
using FluentAssertions;

namespace Employee360.UnitTests.Infrastructure;

/// <summary>Tests for the PBKDF2 <see cref="PasswordHasher"/>.</summary>
public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashAndVerify_ShouldRoundTrip()
    {
        var hash = _hasher.Hash("Str0ng!Passw0rd");

        _hasher.Verify("Str0ng!Passw0rd", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WrongPassword_ShouldFail()
    {
        var hash = _hasher.Hash("Str0ng!Passw0rd");

        _hasher.Verify("Wr0ng!Passw0rd", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_SamePasswordTwice_ShouldProduceDifferentHashes()
    {
        var first = _hasher.Hash("Str0ng!Passw0rd");
        var second = _hasher.Hash("Str0ng!Passw0rd");

        first.Should().NotBe(second); // random salt per hash
    }

    [Fact]
    public void Verify_MalformedHash_ShouldReturnFalse()
    {
        _hasher.Verify("any", "not-a-valid-hash").Should().BeFalse();
        _hasher.Verify("any", "100000.!!!.???").Should().BeFalse();
    }

    [Fact]
    public void Hash_ShouldBeSelfDescribing()
    {
        var hash = _hasher.Hash("Str0ng!Passw0rd");

        hash.Split('.').Should().HaveCount(3);
        hash.Should().StartWith("100000.");
    }
}
