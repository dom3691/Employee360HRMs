using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Application.Features.Auth.Login;
using Employee360.Domain.Entities;
using Employee360.Domain.Interfaces;
using Employee360.Infrastructure.Identity;
using Employee360.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Employee360.UnitTests.Application.Auth;

/// <summary>
/// Tests for <see cref="LoginHandler"/>: credential verification, the
/// 5-attempt / 15-minute lockout policy (FR-AUTH-003), and token issuance.
/// </summary>
public class LoginHandlerTests
{
    private const string Email = "ada.okafor@company.ng";
    private const string CorrectPassword = "Str0ng!Passw0rd";

    private readonly PasswordHasher _hasher = new();
    private DateTime _utcNow = new(2026, 7, 26, 9, 0, 0, DateTimeKind.Utc);

    private Employee360DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"login-tests-{Guid.NewGuid()}")
            .Options;

        return new Employee360DbContext(options);
    }

    private LoginHandler CreateHandler(Employee360DbContext context)
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(() => _utcNow);

        var jwt = new Mock<IJwtTokenService>();
        jwt.Setup(j => j.GenerateAccessToken(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
            .Returns(new AccessTokenResult("access-token", _utcNow.AddMinutes(15)));
        jwt.Setup(j => j.GenerateRefreshToken())
            .Returns(() => new RefreshTokenResult(Guid.NewGuid().ToString("N"), _utcNow.AddDays(7)));

        return new LoginHandler(context, _hasher, jwt.Object, clock.Object);
    }

    private async Task<User> SeedUserAsync(Employee360DbContext context)
    {
        var user = new User
        {
            Email = Email,
            PasswordHash = _hasher.Hash(CorrectPassword),
            IsActive = true,
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task ValidCredentials_ShouldSucceed_AndStoreRefreshToken()
    {
        await using var context = CreateContext();
        await SeedUserAsync(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new LoginCommand(Email, CorrectPassword), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeEmpty();
        (await context.RefreshTokens.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task WrongPassword_ShouldFail_WithGenericMessage()
    {
        await using var context = CreateContext();
        await SeedUserAsync(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new LoginCommand(Email, "Wrong!Pass1"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task FifthFailedAttempt_ShouldLockAccountFor15Minutes()
    {
        await using var context = CreateContext();
        var user = await SeedUserAsync(context);
        var handler = CreateHandler(context);

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            await handler.Handle(new LoginCommand(Email, "Wrong!Pass1"), default);
        }

        user = await context.Users.SingleAsync();
        user.LockoutEndUtc.Should().Be(_utcNow.AddMinutes(LoginHandler.LockoutMinutes));

        // Correct password is still rejected while locked.
        var lockedResult = await handler.Handle(new LoginCommand(Email, CorrectPassword), default);
        lockedResult.IsFailure.Should().BeTrue();
        lockedResult.Error.Should().Contain("locked");
    }

    [Fact]
    public async Task AfterLockoutExpires_CorrectPassword_ShouldSucceed()
    {
        await using var context = CreateContext();
        await SeedUserAsync(context);
        var handler = CreateHandler(context);

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            await handler.Handle(new LoginCommand(Email, "Wrong!Pass1"), default);
        }

        _utcNow = _utcNow.AddMinutes(LoginHandler.LockoutMinutes + 1);

        var result = await handler.Handle(new LoginCommand(Email, CorrectPassword), default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SuccessfulLogin_ShouldResetFailedAttemptCounter()
    {
        await using var context = CreateContext();
        await SeedUserAsync(context);
        var handler = CreateHandler(context);

        await handler.Handle(new LoginCommand(Email, "Wrong!Pass1"), default);
        await handler.Handle(new LoginCommand(Email, "Wrong!Pass1"), default);
        await handler.Handle(new LoginCommand(Email, CorrectPassword), default);

        var user = await context.Users.SingleAsync();
        user.FailedLoginAttempts.Should().Be(0);
        user.LastLoginAt.Should().Be(_utcNow);
    }

    [Fact]
    public async Task DeactivatedAccount_ShouldBeRejected()
    {
        await using var context = CreateContext();
        var user = await SeedUserAsync(context);
        user.IsActive = false;
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(new LoginCommand(Email, CorrectPassword), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("deactivated");
    }

    [Fact]
    public async Task UnknownEmail_ShouldFail_WithGenericMessage()
    {
        await using var context = CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(new LoginCommand("ghost@company.ng", CorrectPassword), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid email or password.");
    }
}
