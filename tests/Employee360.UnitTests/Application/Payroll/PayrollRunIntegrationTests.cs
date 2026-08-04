using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Application.Common.Services;
using Employee360.Domain.Common;
using Employee360.Application.Features.Payroll.Runs;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using Employee360.Infrastructure.BackgroundJobs;
using Employee360.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Employee360.UnitTests.Application.Payroll;

public class PayrollRunIntegrationTests
{
    private static Employee360DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"payroll-run-{Guid.NewGuid()}")
            .Options;

        return new Employee360DbContext(options);
    }

    private static async Task SeedPayrollConfigAsync(Employee360DbContext context)
    {
        foreach (var band in NigeriaPayrollCalculatorGoldenTests.ReferenceTaxBands())
        {
            context.TaxBands.Add(new TaxBand
            {
                TaxYear = 2026,
                BandOrder = band.BandOrder,
                UpperBoundAnnual = band.UpperBoundAnnual,
                Rate = band.Rate,
                IsActive = true,
            });
        }

        foreach (var rate in NigeriaPayrollCalculatorGoldenTests.ReferenceStatutoryRates())
        {
            context.PayrollStatutoryRates.Add(new PayrollStatutoryRate
            {
                Code = rate.Code,
                Name = rate.Code,
                TaxYear = 2026,
                EmployeeRate = rate.EmployeeRate,
                EmployerRate = rate.EmployerRate,
                FixedAnnualAmount = rate.FixedAnnualAmount,
                VariableRate = rate.VariableRate,
                Basis = rate.Basis,
                IsActive = true,
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task<(Employee Employee, PayrollRun Run)> SeedEmployeeAndRunAsync(
        Employee360DbContext context)
    {
        await SeedPayrollConfigAsync(context);

        var employee = new Employee
        {
            EmployeeCode = "EMP-001",
            FirstName = "Ada",
            LastName = "Okafor",
            Email = "ada@company.ng",
            Status = EmployeeStatus.Active,
        };

        context.Employees.Add(employee);
        context.EmployeeSalaries.Add(new EmployeeSalary
        {
            EmployeeId = employee.Id,
            EffectiveDate = new DateOnly(2026, 1, 1),
            Basic = 150_000m,
            Housing = 50_000m,
            Transport = 30_000m,
            OtherAllowances = 20_000m,
        });

        var run = new PayrollRun
        {
            PeriodYear = 2026,
            PeriodMonth = 8,
            PeriodStart = new DateOnly(2026, 8, 1),
            PeriodEnd = new DateOnly(2026, 8, 31),
            PeriodLabel = "August 2026",
            TaxYear = 2026,
            Status = PayrollRunStatus.Draft,
        };

        context.PayrollRuns.Add(run);
        await context.SaveChangesAsync();

        return (employee, run);
    }

    [Fact]
    public async Task FullWorkflow_DraftThroughFinalized_ShouldTransitionCorrectly()
    {
        await using var context = CreateContext();
        var (_, run) = await SeedEmployeeAndRunAsync(context);

        var calcService = new EmployeePayrollCalculationService(
            context,
            new NigeriaPayrollCalculator(),
            Options.Create(new PayrollSettings()));

        var calcJob = new CalculatePayrollJob(
            context, calcService, NullLogger<CalculatePayrollJob>.Instance);

        await calcJob.RunAsync(run.Id);

        var calculated = await context.PayrollRuns.SingleAsync();
        calculated.Status.Should().Be(PayrollRunStatus.Calculated);
        calculated.ProcessedEmployees.Should().Be(1);
        (await context.Payslips.CountAsync()).Should().Be(1);

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(new DateTime(2026, 8, 28, 10, 0, 0, DateTimeKind.Utc));

        var submitHandler = new SubmitPayrollRunForApprovalHandler(
            context,
            Mock.Of<IEmailService>(),
            Mock.Of<IEmailTemplateService>(),
            clock.Object);

        (await submitHandler.Handle(new SubmitPayrollRunForApprovalCommand(run.Id), default))
            .IsSuccess.Should().BeTrue();

        var approveHandler = new ApprovePayrollRunHandler(
            context,
            Mock.Of<ICurrentUserService>(),
            clock.Object);

        (await approveHandler.Handle(new ApprovePayrollRunCommand(run.Id), default))
            .IsSuccess.Should().BeTrue();

        var payslipStorage = new Mock<IPayslipStorageService>();
        payslipStorage
            .Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("path.pdf");

        var finalizeHandler = new FinalizePayrollRunHandler(
            context,
            new PayslipPdfGenerator(),
            payslipStorage.Object,
            Mock.Of<IEmailService>(),
            Mock.Of<IEmailTemplateService>(),
            clock.Object);

        context.Companies.Add(new Company { Name = "Acme Nigeria Ltd" });
        await context.SaveChangesAsync();

        (await finalizeHandler.Handle(new FinalizePayrollRunCommand(run.Id), default))
            .IsSuccess.Should().BeTrue();

        var finalized = await context.PayrollRuns.SingleAsync();
        finalized.Status.Should().Be(PayrollRunStatus.Finalized);
        finalized.FinalizedAtUtc.Should().NotBeNull();

        var initiateHandler = new InitiatePayrollRunHandler(context, Mock.Of<ICurrentUserService>());
        var lockResult = await initiateHandler.Handle(
            new InitiatePayrollRunCommand(2026, 8, null), default);

        lockResult.IsFailure.Should().BeTrue();
        lockResult.Error.Should().Contain("finalized");
    }

    [Fact]
    public async Task CalculateJob_MidRunFailure_ShouldRollbackPayslips()
    {
        await using var context = CreateContext();
        await SeedPayrollConfigAsync(context);

        var employee1 = new Employee
        {
            EmployeeCode = "E1", FirstName = "A", LastName = "One",
            Email = "a@co.ng", Status = EmployeeStatus.Active,
        };
        var employee2 = new Employee
        {
            EmployeeCode = "E2", FirstName = "B", LastName = "Two",
            Email = "b@co.ng", Status = EmployeeStatus.Active,
        };

        context.Employees.AddRange(employee1, employee2);
        context.EmployeeSalaries.AddRange(
            new EmployeeSalary
            {
                EmployeeId = employee1.Id,
                EffectiveDate = new DateOnly(2026, 1, 1),
                Basic = 150_000m, Housing = 50_000m, Transport = 30_000m, OtherAllowances = 20_000m,
            },
            new EmployeeSalary
            {
                EmployeeId = employee2.Id,
                EffectiveDate = new DateOnly(2026, 1, 1),
                Basic = 150_000m, Housing = 50_000m, Transport = 30_000m, OtherAllowances = 20_000m,
            });

        var run = new PayrollRun
        {
            PeriodYear = 2026,
            PeriodMonth = 7,
            PeriodStart = new DateOnly(2026, 7, 1),
            PeriodEnd = new DateOnly(2026, 7, 31),
            PeriodLabel = "July 2026",
            TaxYear = 2026,
            Status = PayrollRunStatus.Draft,
        };

        context.PayrollRuns.Add(run);
        await context.SaveChangesAsync();

        var calcMock = new Mock<IEmployeePayrollCalculationService>();
        calcMock
            .SetupSequence(s => s.CalculateForEmployeeAsync(
                It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new PayrollCalculationResult
            {
                Basic = 150_000m, Housing = 50_000m, Transport = 30_000m, OtherAllowances = 20_000m,
                GrossPay = 250_000m, NetPay = 210_350m,
            }))
            .ReturnsAsync(Result.Failure<PayrollCalculationResult>("Simulated failure"));

        var job = new CalculatePayrollJob(context, calcMock.Object, NullLogger<CalculatePayrollJob>.Instance);

        var act = () => job.RunAsync(run.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();

        var reloaded = await context.PayrollRuns.SingleAsync();
        reloaded.Status.Should().Be(PayrollRunStatus.Draft);
        reloaded.LastError.Should().Contain("Simulated failure");
        (await context.Payslips.CountAsync()).Should().Be(0);
    }
}
