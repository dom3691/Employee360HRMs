using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Employee360.Domain.Interfaces;
using Employee360.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace Employee360.UnitTests.Infrastructure;

/// <summary>Tests for <see cref="JwtTokenService"/> token generation and inspection.</summary>
public class JwtTokenServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();

    private static JwtTokenService CreateService()
    {
        var settings = new JwtSettings
        {
            Issuer = "Employee360.Tests",
            Audience = "Employee360.TestClient",
            SigningKey = "unit-test-signing-key-with-at-least-32-characters!",
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7,
        };

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(DateTime.UtcNow);

        return new JwtTokenService(Options.Create(settings), clock.Object);
    }

    [Fact]
    public void GenerateAccessToken_ShouldEmbedIdentityRoleAndPermissionClaims()
    {
        var service = CreateService();

        var result = service.GenerateAccessToken(
            UserId,
            "ada.okafor@company.ng",
            EmployeeId,
            roles: ["LineManager"],
            permissions: ["Leave.ApproveTeam", "Employees.ViewTeam"]);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == UserId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "ada.okafor@company.ng");
        jwt.Claims.Should().Contain(c => c.Type == "employee_id" && c.Value == EmployeeId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "LineManager");
        jwt.Claims.Where(c => c.Type == "permission").Select(c => c.Value)
            .Should().BeEquivalentTo("Leave.ApproveTeam", "Employees.ViewTeam");
    }

    [Fact]
    public void GenerateAccessToken_ShouldExpirePerSettings()
    {
        var service = CreateService();

        var result = service.GenerateAccessToken(UserId, "a@b.ng", null, [], []);

        result.ExpiresAtUtc.Should().BeCloseTo(
            DateTime.UtcNow.AddMinutes(15),
            TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateRefreshToken_ShouldBeUniqueHighEntropy_WithConfiguredExpiry()
    {
        var service = CreateService();

        var tokens = Enumerable.Range(0, 50).Select(_ => service.GenerateRefreshToken()).ToList();

        tokens.Select(t => t.Token).Should().OnlyHaveUniqueItems();
        Convert.FromBase64String(tokens[0].Token).Should().HaveCount(64);
        tokens[0].ExpiresAtUtc.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ShouldReturnPrincipal_ForValidSignature()
    {
        var service = CreateService();
        var token = service.GenerateAccessToken(UserId, "a@b.ng", null, ["Employee"], []).Token;

        var principal = service.GetPrincipalFromExpiredToken(token);

        principal.Should().NotBeNull();
        principal!.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be(UserId.ToString());
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ShouldReturnNull_ForTamperedToken()
    {
        var service = CreateService();
        var token = service.GenerateAccessToken(UserId, "a@b.ng", null, [], []).Token;

        var principal = service.GetPrincipalFromExpiredToken(token + "tampered");

        principal.Should().BeNull();
    }
}
