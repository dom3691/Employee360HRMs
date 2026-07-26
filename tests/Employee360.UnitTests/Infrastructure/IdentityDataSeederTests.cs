using Employee360.Domain.Constants;
using Employee360.Infrastructure.Persistence;
using Employee360.Infrastructure.Persistence.Seeding;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Employee360.UnitTests.Infrastructure;

/// <summary>Tests for <see cref="IdentityDataSeeder"/> (PRD RBAC matrix, Appendix A).</summary>
public class IdentityDataSeederTests
{
    private static Employee360DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"seeder-tests-{Guid.NewGuid()}")
            .Options;

        return new Employee360DbContext(options);
    }

    private static IdentityDataSeeder CreateSeeder(Employee360DbContext context) =>
        new(context, NullLogger<IdentityDataSeeder>.Instance);

    [Fact]
    public async Task Seed_ShouldCreateAllSystemRoles()
    {
        await using var context = CreateContext();

        await CreateSeeder(context).SeedAsync();

        var roleNames = await context.Roles.Select(r => r.Name).ToListAsync();
        roleNames.Should().BeEquivalentTo(RoleNames.All);
        (await context.Roles.AllAsync(r => r.IsSystemRole)).Should().BeTrue();
    }

    [Fact]
    public async Task Seed_ShouldCreateFullPermissionCatalog()
    {
        await using var context = CreateContext();

        await CreateSeeder(context).SeedAsync();

        var count = await context.Permissions.CountAsync();
        count.Should().Be(Permissions.GetAll().Count);
    }

    [Fact]
    public async Task Seed_ShouldGrantMatrixPermissions()
    {
        await using var context = CreateContext();

        await CreateSeeder(context).SeedAsync();

        async Task<List<string>> PermissionsOf(string roleName) =>
            await context.RolePermissions
                .Where(rp => rp.Role.Name == roleName)
                .Select(rp => rp.Permission.Name)
                .ToListAsync();

        (await PermissionsOf(RoleNames.HRAdmin)).Should().Contain(Permissions.Employees.Create);
        (await PermissionsOf(RoleNames.Employee)).Should().NotContain(Permissions.Employees.Create);
        (await PermissionsOf(RoleNames.LineManager)).Should().Contain(Permissions.Leave.ApproveTeam);
        (await PermissionsOf(RoleNames.PayrollOfficer)).Should().Contain(Permissions.Payroll.ExportBankFile);
        (await PermissionsOf(RoleNames.HRManager)).Should().NotContain(Permissions.Payroll.ExportBankFile);
        (await PermissionsOf(RoleNames.SystemAdmin)).Should().Contain(Permissions.Administration.ManageRoles);
        (await PermissionsOf(RoleNames.Executive)).Should().BeEquivalentTo([Permissions.Reports.ViewExecutive]);
    }

    [Fact]
    public async Task Seed_RunTwice_ShouldBeIdempotent()
    {
        await using var context = CreateContext();
        var seeder = CreateSeeder(context);

        await seeder.SeedAsync();
        var rolesAfterFirst = await context.Roles.CountAsync();
        var permissionsAfterFirst = await context.Permissions.CountAsync();
        var grantsAfterFirst = await context.RolePermissions.CountAsync();

        await seeder.SeedAsync();

        (await context.Roles.CountAsync()).Should().Be(rolesAfterFirst);
        (await context.Permissions.CountAsync()).Should().Be(permissionsAfterFirst);
        (await context.RolePermissions.CountAsync()).Should().Be(grantsAfterFirst);
    }
}
