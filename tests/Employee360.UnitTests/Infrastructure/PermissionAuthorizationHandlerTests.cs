using System.Security.Claims;
using Employee360.Infrastructure.Identity;
using Employee360.Infrastructure.Identity.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

namespace Employee360.UnitTests.Infrastructure;

/// <summary>Tests for <see cref="PermissionAuthorizationHandler"/>.</summary>
public class PermissionAuthorizationHandlerTests
{
    private static AuthorizationHandlerContext CreateContext(
        string requiredPermission,
        params string[] grantedPermissions)
    {
        var claims = grantedPermissions
            .Select(p => new Claim(JwtTokenService.PermissionClaimType, p))
            .ToList();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var requirement = new PermissionRequirement(requiredPermission);

        return new AuthorizationHandlerContext([requirement], principal, resource: null);
    }

    [Fact]
    public async Task User_WithRequiredPermission_ShouldSucceed()
    {
        var handler = new PermissionAuthorizationHandler();
        var context = CreateContext("Employees.Create", "Employees.Create", "Leave.Apply");

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task User_WithoutRequiredPermission_ShouldNotSucceed()
    {
        var handler = new PermissionAuthorizationHandler();
        var context = CreateContext("Employees.Create", "Leave.Apply");

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task PermissionComparison_ShouldBeCaseSensitive()
    {
        var handler = new PermissionAuthorizationHandler();
        var context = CreateContext("Employees.Create", "employees.create");

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }
}
