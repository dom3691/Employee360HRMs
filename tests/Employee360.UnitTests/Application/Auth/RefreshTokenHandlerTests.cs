using Employee360.Application.Features.Auth.RefreshToken;
using Employee360.Domain.Entities;
using Employee360.Domain.Interfaces;
using Employee360.Infrastructure.Identity;
using Employee360.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace Employee360.UnitTests.Application.Auth;

/// <summary>
/// Tests for <see cref="RefreshTokenHandler"/> single-use rotation using the real
/// <see cref="JwtTokenService"/> for token generation and principal extraction.
/// </summary>
public class RefreshTokenHandlerTests
{
    private readonly DateTime _utcNow = DateTime.UtcNow;
    private readonly JwtTokenService _jwtService;

    public RefreshTokenHandlerTests()
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
        clock.SetupGet(c => c.UtcNow).Returns(() => _utcNow);

        _jwtService = new JwtTokenService(Options.Create(settings), clock.Object);
    }

    private Employee360DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"refresh-tests-{Guid.NewGuid()}")
            .Options;

        return new Employee360DbContext(options);
    }

    private RefreshTokenHandler CreateHandler(Employee360DbContext context)
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(() => _utcNow);

        return new RefreshTokenHandler(context, _jwtService, clock.Object);
    }

    private async Task<(User User, string AccessToken, string RefreshToken)> SeedAuthenticatedUserAsync(
        Employee360DbContext context)
    {
        var user = new User
        {
            Email = "ada.okafor@company.ng",
            PasswordHash = "irrelevant",
            IsActive = true,
        };
        context.Users.Add(user);

        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, null, [], []);
        var refreshToken = _jwtService.GenerateRefreshToken();

        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken.Token,
            ExpiresAtUtc = refreshToken.ExpiresAtUtc,
            CreatedAtUtc = _utcNow,
        });

        await context.SaveChangesAsync();
        return (user, accessToken.Token, refreshToken.Token);
    }

    [Fact]
    public async Task ValidPair_ShouldRotate_RevokingOldToken()
    {
        await using var context = CreateContext();
        var (_, accessToken, refreshToken) = await SeedAuthenticatedUserAsync(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new RefreshTokenCommand(accessToken, refreshToken), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.RefreshToken.Should().NotBe(refreshToken);

        var oldToken = await context.RefreshTokens.SingleAsync(rt => rt.Token == refreshToken);
        oldToken.RevokedAtUtc.Should().Be(_utcNow);
        oldToken.ReplacedByToken.Should().Be(result.Value.RefreshToken);

        var newToken = await context.RefreshTokens.SingleAsync(rt => rt.Token == result.Value.RefreshToken);
        newToken.RevokedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task ReusingRotatedToken_ShouldFail()
    {
        await using var context = CreateContext();
        var (_, accessToken, refreshToken) = await SeedAuthenticatedUserAsync(context);
        var handler = CreateHandler(context);

        var first = await handler.Handle(new RefreshTokenCommand(accessToken, refreshToken), default);
        first.IsSuccess.Should().BeTrue();

        var replay = await handler.Handle(new RefreshTokenCommand(accessToken, refreshToken), default);

        replay.IsFailure.Should().BeTrue();
        replay.Error.Should().Contain("Invalid or expired refresh token");
    }

    [Fact]
    public async Task RefreshTokenOfDifferentUser_ShouldFail()
    {
        await using var context = CreateContext();
        var (_, _, refreshToken) = await SeedAuthenticatedUserAsync(context);

        // Access token belonging to a different user id.
        var strangerAccessToken = _jwtService
            .GenerateAccessToken(Guid.NewGuid(), "stranger@company.ng", null, [], [])
            .Token;

        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new RefreshTokenCommand(strangerAccessToken, refreshToken), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task TamperedAccessToken_ShouldFail()
    {
        await using var context = CreateContext();
        var (_, accessToken, refreshToken) = await SeedAuthenticatedUserAsync(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new RefreshTokenCommand(accessToken + "x", refreshToken), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid access token.");
    }

    [Fact]
    public async Task ExpiredRefreshToken_ShouldFail()
    {
        await using var context = CreateContext();
        var (user, accessToken, _) = await SeedAuthenticatedUserAsync(context);

        var expired = new RefreshToken
        {
            UserId = user.Id,
            Token = "expired-token",
            ExpiresAtUtc = _utcNow.AddMinutes(-1),
            CreatedAtUtc = _utcNow.AddDays(-8),
        };
        context.RefreshTokens.Add(expired);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new RefreshTokenCommand(accessToken, "expired-token"), default);

        result.IsFailure.Should().BeTrue();
    }
}
