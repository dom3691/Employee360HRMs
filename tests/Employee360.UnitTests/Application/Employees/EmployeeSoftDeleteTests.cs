using Employee360.Domain.Entities;
using Employee360.Domain.Interfaces;
using Employee360.Infrastructure.Persistence;
using Employee360.Infrastructure.Persistence.Interceptors;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Employee360.UnitTests.Application.Employees;

/// <summary>
/// Soft-delete behavior for employees (FR-EMP-005): removal converts to a soft
/// delete, filtered rows disappear from queries, and codes are never reused.
/// </summary>
public class EmployeeSoftDeleteTests
{
    private static Employee360DbContext CreateContext()
    {
        var currentUser = new Mock<ICurrentUserService>();
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(DateTime.UtcNow);

        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"emp-softdelete-{Guid.NewGuid()}")
            .AddInterceptors(new AuditableEntityInterceptor(currentUser.Object, clock.Object))
            .Options;

        return new Employee360DbContext(options);
    }

    private static Employee NewEmployee(string code, string email) => new()
    {
        EmployeeCode = code,
        FirstName = "Test",
        LastName = "Employee",
        Email = email,
    };

    [Fact]
    public async Task RemovedEmployee_ShouldBeSoftDeleted_AndExcludedFromQueries()
    {
        await using var context = CreateContext();
        var employee = NewEmployee("EMP-00001", "a@company.ng");
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        context.Employees.Remove(employee);
        await context.SaveChangesAsync();

        (await context.Employees.ToListAsync()).Should().BeEmpty();

        var stored = await context.Employees
            .IgnoreQueryFilters()
            .SingleAsync(e => e.Id == employee.Id);
        stored.IsDeleted.Should().BeTrue();
        stored.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SoftDeletedEmployeeCode_ShouldNotBeReused()
    {
        await using var context = CreateContext();
        var employee = NewEmployee("EMP-00001", "a@company.ng");
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        context.Employees.Remove(employee);
        await context.SaveChangesAsync();

        // Code generation scans IgnoreQueryFilters, so the next code is EMP-00002.
        var handler = new Employee360.Application.Features.Employees.CreateEmployee.CreateEmployeeHandler(
            context,
            Employee360.UnitTests.Infrastructure.AesEncryptionServiceTests.CreateService(),
            Microsoft.Extensions.Options.Options.Create(
                new Employee360.Application.Common.Models.EmployeeSettings()));

        var result = await handler.Handle(
            new Employee360.Application.Features.Employees.CreateEmployee.CreateEmployeeCommand(
                "New", null, "Hire", "b@company.ng", null, null,
                Employee360.Domain.Enums.Gender.Male,
                Employee360.Domain.Enums.MaritalStatus.Single,
                null, null, null, null, null, null,
                Employee360.Domain.Enums.EmploymentType.FullTime,
                null, null, null, null),
            default);

        result.Value.EmployeeCode.Should().Be("EMP-00002");
    }
}
