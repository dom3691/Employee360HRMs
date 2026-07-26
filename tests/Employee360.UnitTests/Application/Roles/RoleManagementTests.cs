using Employee360.Application.Features.Roles.AssignRole;
using Employee360.Application.Features.Roles.CreateRole;
using Employee360.Application.Features.Roles.RemoveRole;
using Employee360.Application.Features.Roles.UpdateRolePermissions;
using Employee360.Domain.Entities;
using Employee360.Domain.Interfaces;
using Employee360.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Employee360.UnitTests.Application.Roles;

/// <summary>
/// Tests for the role management slices (System Admin operations) including
/// their explicit audit-log entries.
/// </summary>
public class RoleManagementTests
{
    private static readonly Guid ActingAdminId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 7, 26, 10, 0, 0, DateTimeKind.Utc);

    private static Employee360DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"roles-tests-{Guid.NewGuid()}")
            .Options;

        return new Employee360DbContext(options);
    }

    private static (Mock<ICurrentUserService> User, Mock<IDateTimeProvider> Clock) CreateMocks()
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(s => s.UserId).Returns(ActingAdminId);

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(UtcNow);

        return (currentUser, clock);
    }

    [Fact]
    public async Task CreateRole_ShouldPersistRole()
    {
        await using var context = CreateContext();
        var handler = new CreateRoleHandler(context);

        var result = await handler.Handle(
            new CreateRoleCommand("BranchManager", "Manages a branch office."), default);

        result.IsSuccess.Should().BeTrue();
        (await context.Roles.SingleAsync()).Name.Should().Be("BranchManager");
    }

    [Fact]
    public async Task CreateRole_DuplicateName_ShouldFail()
    {
        await using var context = CreateContext();
        context.Roles.Add(new Role { Name = "BranchManager", IsSystemRole = false });
        await context.SaveChangesAsync();
        var handler = new CreateRoleHandler(context);

        var result = await handler.Handle(
            new CreateRoleCommand("branchmanager", "Duplicate, different casing."), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task AssignRole_ShouldCreateAssignment_AndAuditLog()
    {
        await using var context = CreateContext();
        var user = new User { Email = "a@b.ng", PasswordHash = "x", IsActive = true };
        var role = new Role { Name = "HRAdmin", IsSystemRole = true };
        context.Users.Add(user);
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var (currentUser, clock) = CreateMocks();
        var handler = new AssignRoleHandler(context, currentUser.Object, clock.Object);

        var result = await handler.Handle(new AssignRoleCommand(user.Id, "HRAdmin"), default);

        result.IsSuccess.Should().BeTrue();
        (await context.UserRoles.CountAsync()).Should().Be(1);

        var audit = await context.AuditLogs.SingleAsync(a => a.Action == "RoleAssigned");
        audit.UserId.Should().Be(ActingAdminId);
        audit.NewValues.Should().Contain("HRAdmin");
    }

    [Fact]
    public async Task AssignRole_AlreadyAssigned_ShouldFail()
    {
        await using var context = CreateContext();
        var user = new User { Email = "a@b.ng", PasswordHash = "x", IsActive = true };
        var role = new Role { Name = "HRAdmin", IsSystemRole = true };
        context.Users.Add(user);
        context.Roles.Add(role);
        context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await context.SaveChangesAsync();

        var (currentUser, clock) = CreateMocks();
        var handler = new AssignRoleHandler(context, currentUser.Object, clock.Object);

        var result = await handler.Handle(new AssignRoleCommand(user.Id, "HRAdmin"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already holds");
    }

    [Fact]
    public async Task RemoveRole_ShouldDeleteAssignment_AndAuditLog()
    {
        await using var context = CreateContext();
        var user = new User { Email = "a@b.ng", PasswordHash = "x", IsActive = true };
        var role = new Role { Name = "HRAdmin", IsSystemRole = true };
        context.Users.Add(user);
        context.Roles.Add(role);
        context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await context.SaveChangesAsync();

        var (currentUser, clock) = CreateMocks();
        var handler = new RemoveRoleHandler(context, currentUser.Object, clock.Object);

        var result = await handler.Handle(new RemoveRoleCommand(user.Id, "HRAdmin"), default);

        result.IsSuccess.Should().BeTrue();
        (await context.UserRoles.CountAsync()).Should().Be(0);
        (await context.AuditLogs.AnyAsync(a => a.Action == "RoleRemoved")).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateRolePermissions_ShouldReplaceSet_AndAuditBeforeAfter()
    {
        await using var context = CreateContext();
        var role = new Role { Name = "BranchManager", IsSystemRole = false };
        var oldPermission = new Permission { Name = "Leave.Apply", Module = "Leave" };
        var newPermission = new Permission { Name = "Leave.ApproveTeam", Module = "Leave" };
        context.Roles.Add(role);
        context.Permissions.AddRange(oldPermission, newPermission);
        context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = oldPermission.Id });
        await context.SaveChangesAsync();

        var (currentUser, clock) = CreateMocks();
        var handler = new UpdateRolePermissionsHandler(context, currentUser.Object, clock.Object);

        var result = await handler.Handle(
            new UpdateRolePermissionsCommand(role.Id, ["Leave.ApproveTeam"]), default);

        result.IsSuccess.Should().BeTrue();

        var grants = await context.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.Permission.Name)
            .ToListAsync();
        grants.Should().BeEquivalentTo(["Leave.ApproveTeam"]);

        var audit = await context.AuditLogs.SingleAsync(a => a.Action == "PermissionsUpdated");
        audit.OldValues.Should().Contain("Leave.Apply");
        audit.NewValues.Should().Contain("Leave.ApproveTeam");
    }

    [Fact]
    public async Task UpdateRolePermissions_UnknownPermission_ShouldFail()
    {
        await using var context = CreateContext();
        var role = new Role { Name = "BranchManager", IsSystemRole = false };
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var (currentUser, clock) = CreateMocks();
        var handler = new UpdateRolePermissionsHandler(context, currentUser.Object, clock.Object);

        var result = await handler.Handle(
            new UpdateRolePermissionsCommand(role.Id, ["Nonexistent.Permission"]), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Unknown permissions");
    }
}
