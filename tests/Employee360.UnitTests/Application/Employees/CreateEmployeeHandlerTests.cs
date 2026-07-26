using Employee360.Application.Common.Models;
using Employee360.Application.Features.Employees.CreateEmployee;
using Employee360.Domain.Enums;
using Employee360.Infrastructure.Persistence;
using Employee360.UnitTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Employee360.UnitTests.Application.Employees;

/// <summary>
/// Tests for <see cref="CreateEmployeeHandler"/>: employee code generation
/// (FR-EMP-001), PII encryption (NFR-SEC-004), and duplicate protection.
/// </summary>
public class CreateEmployeeHandlerTests
{
    private static Employee360DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"create-emp-{Guid.NewGuid()}")
            .Options;

        return new Employee360DbContext(options);
    }

    private static CreateEmployeeHandler CreateHandler(
        Employee360DbContext context,
        EmployeeSettings? settings = null)
        => new(
            context,
            AesEncryptionServiceTests.CreateService(),
            Options.Create(settings ?? new EmployeeSettings()));

    private static CreateEmployeeCommand NewCommand(
        string email = "ada.okafor@company.ng",
        string? nin = null,
        BankAccountInput? bank = null)
        => new(
            "Ada", null, "Okafor", email, "+2348012345678",
            new DateOnly(1990, 5, 10), Gender.Female, MaritalStatus.Single,
            "Nigerian", nin, "12 Marina Rd, Lagos", null, null, null,
            EmploymentType.FullTime, new DateOnly(2026, 8, 1), "Lagos HQ",
            bank, null);

    [Fact]
    public async Task Create_ShouldGenerateSequentialCodes()
    {
        await using var context = CreateContext();
        var handler = CreateHandler(context);

        var first = await handler.Handle(NewCommand("a@company.ng"), default);
        var second = await handler.Handle(NewCommand("b@company.ng"), default);

        first.Value.EmployeeCode.Should().Be("EMP-00001");
        second.Value.EmployeeCode.Should().Be("EMP-00002");
    }

    [Fact]
    public async Task Create_WithCustomCodeFormat_ShouldRespectSettings()
    {
        await using var context = CreateContext();
        var handler = CreateHandler(context, new EmployeeSettings { CodePrefix = "E360", CodeDigits = 4 });

        var result = await handler.Handle(NewCommand(), default);

        result.Value.EmployeeCode.Should().Be("E360-0001");
    }

    [Fact]
    public async Task Create_ShouldSaveAsActive()
    {
        await using var context = CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(NewCommand(), default);

        var employee = await context.Employees.SingleAsync(e => e.Id == result.Value.Id);
        employee.Status.Should().Be(EmployeeStatus.Active);
    }

    [Fact]
    public async Task Create_DuplicateEmail_ShouldFail()
    {
        await using var context = CreateContext();
        var handler = CreateHandler(context);
        await handler.Handle(NewCommand("same@company.ng"), default);

        var duplicate = await handler.Handle(NewCommand("SAME@company.ng"), default);

        duplicate.IsFailure.Should().BeTrue();
        duplicate.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Create_ShouldEncryptNinAndAccountNumber_AtRest()
    {
        await using var context = CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            NewCommand(nin: "12345678901", bank: new BankAccountInput("GTBank", "0123456789", "Ada Okafor")),
            default);

        var employee = await context.Employees
            .Include(e => e.BankAccount)
            .SingleAsync(e => e.Id == result.Value.Id);

        employee.NinEncrypted.Should().NotBeNullOrEmpty()
            .And.NotContain("12345678901");
        employee.BankAccount!.AccountNumberEncrypted.Should().NotBeNullOrEmpty()
            .And.NotContain("0123456789");

        // Round-trip through the same key restores the plaintext.
        var encryption = AesEncryptionServiceTests.CreateService();
        encryption.Decrypt(employee.NinEncrypted!).Should().Be("12345678901");
        encryption.Decrypt(employee.BankAccount.AccountNumberEncrypted).Should().Be("0123456789");
    }

    [Fact]
    public async Task Create_WithUnknownManager_ShouldFail()
    {
        await using var context = CreateContext();
        var handler = CreateHandler(context);

        var command = NewCommand() with { ManagerId = Guid.NewGuid() };
        var result = await handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Manager not found.");
    }
}
