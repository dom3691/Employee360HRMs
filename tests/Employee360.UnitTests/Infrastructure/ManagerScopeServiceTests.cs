using Employee360.Domain.Entities;
using Employee360.Infrastructure.Identity;
using Employee360.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Employee360.UnitTests.Infrastructure;

/// <summary>
/// Tests for <see cref="ManagerScopeService"/> hierarchy resolution.
/// Hierarchy under test:
///   CEO -> (HeadOfSales, HeadOfEng)
///   HeadOfSales -> (Rep1)
///   HeadOfEng   -> (Dev1, Dev2); Dev1 -> (Intern)
/// </summary>
public class ManagerScopeServiceTests
{
    private static readonly Guid Ceo = Guid.NewGuid();
    private static readonly Guid HeadOfSales = Guid.NewGuid();
    private static readonly Guid HeadOfEng = Guid.NewGuid();
    private static readonly Guid Rep1 = Guid.NewGuid();
    private static readonly Guid Dev1 = Guid.NewGuid();
    private static readonly Guid Dev2 = Guid.NewGuid();
    private static readonly Guid Intern = Guid.NewGuid();

    private static async Task<Employee360DbContext> CreateContextWithHierarchyAsync()
    {
        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"scope-tests-{Guid.NewGuid()}")
            .Options;

        var context = new Employee360DbContext(options);

        Employee Make(Guid id, string name, Guid? managerId) => new()
        {
            Id = id,
            FirstName = name,
            LastName = "Test",
            Email = $"{name.ToLowerInvariant()}@company.ng",
            ManagerId = managerId,
        };

        context.Employees.AddRange(
            Make(Ceo, "Ceo", null),
            Make(HeadOfSales, "Sales", Ceo),
            Make(HeadOfEng, "Eng", Ceo),
            Make(Rep1, "Rep", HeadOfSales),
            Make(Dev1, "DevOne", HeadOfEng),
            Make(Dev2, "DevTwo", HeadOfEng),
            Make(Intern, "Intern", Dev1));

        await context.SaveChangesAsync();
        return context;
    }

    [Fact]
    public async Task GetManagedEmployeeIds_DirectOnly_ShouldReturnDirectReports()
    {
        await using var context = await CreateContextWithHierarchyAsync();
        var service = new ManagerScopeService(context);

        var managed = await service.GetManagedEmployeeIdsAsync(HeadOfEng, includeIndirect: false);

        managed.Should().BeEquivalentTo([Dev1, Dev2]);
    }

    [Fact]
    public async Task GetManagedEmployeeIds_WithIndirect_ShouldReturnFullSubtree()
    {
        await using var context = await CreateContextWithHierarchyAsync();
        var service = new ManagerScopeService(context);

        var managed = await service.GetManagedEmployeeIdsAsync(HeadOfEng);

        managed.Should().BeEquivalentTo([Dev1, Dev2, Intern]);
    }

    [Fact]
    public async Task GetManagedEmployeeIds_ForTopManager_ShouldReturnEveryoneBelow()
    {
        await using var context = await CreateContextWithHierarchyAsync();
        var service = new ManagerScopeService(context);

        var managed = await service.GetManagedEmployeeIdsAsync(Ceo);

        managed.Should().HaveCount(6).And.NotContain(Ceo);
    }

    [Fact]
    public async Task GetManagedEmployeeIds_ForNonManager_ShouldBeEmpty()
    {
        await using var context = await CreateContextWithHierarchyAsync();
        var service = new ManagerScopeService(context);

        var managed = await service.GetManagedEmployeeIdsAsync(Intern);

        managed.Should().BeEmpty();
    }

    [Fact]
    public async Task IsManagerOf_ShouldDetectIndirectReport()
    {
        await using var context = await CreateContextWithHierarchyAsync();
        var service = new ManagerScopeService(context);

        (await service.IsManagerOfAsync(HeadOfEng, Intern)).Should().BeTrue();
        (await service.IsManagerOfAsync(HeadOfSales, Intern)).Should().BeFalse();
    }
}
