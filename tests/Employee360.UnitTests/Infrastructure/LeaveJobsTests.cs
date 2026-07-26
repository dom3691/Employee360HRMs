using Employee360.Application.Common.Models;
using Employee360.Application.Features.Leave.Common;
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

namespace Employee360.UnitTests.Infrastructure;

/// <summary>
/// Tests for the recurring leave jobs: escalation trigger (FR-LV-009) and
/// accrual with carry-forward cap (FR-LV-002).
/// </summary>
public class LeaveJobsTests
{
    private static readonly DateTime UtcNow = new(2026, 7, 20, 9, 0, 0, DateTimeKind.Utc);

    private static Employee360DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"leave-jobs-{Guid.NewGuid()}")
            .Options;

        return new Employee360DbContext(options);
    }

    private static Mock<IDateTimeProvider> Clock(DateTime? utcNow = null, DateOnly? today = null)
    {
        var mock = new Mock<IDateTimeProvider>();
        mock.SetupGet(c => c.UtcNow).Returns(utcNow ?? UtcNow);
        mock.SetupGet(c => c.TodayWat).Returns(today ?? DateOnly.FromDateTime((utcNow ?? UtcNow).AddHours(1)));
        return mock;
    }

    // -----------------------------------------------------------------------
    // Escalation
    // -----------------------------------------------------------------------

    private static async Task<(Employee GrandBoss, Employee Manager, Employee Worker, LeaveRequest Request)>
        SeedPendingRequestAsync(Employee360DbContext context, DateTime createdAt)
    {
        var grandBoss = new Employee
        {
            EmployeeCode = "E1", FirstName = "Grand", LastName = "Boss",
            Email = "gb@company.ng", Status = EmployeeStatus.Active,
        };
        var manager = new Employee
        {
            EmployeeCode = "E2", FirstName = "Line", LastName = "Manager",
            Email = "lm@company.ng", ManagerId = grandBoss.Id, Status = EmployeeStatus.Active,
        };
        var worker = new Employee
        {
            EmployeeCode = "E3", FirstName = "Ada", LastName = "Okafor",
            Email = "ada@company.ng", ManagerId = manager.Id, Status = EmployeeStatus.Active,
        };
        var leaveType = new LeaveType { Name = "Annual Leave", Code = "ANN", IsPaid = true };

        var request = new LeaveRequest
        {
            EmployeeId = worker.Id,
            LeaveTypeId = leaveType.Id,
            StartDate = new DateOnly(2026, 8, 3),
            EndDate = new DateOnly(2026, 8, 5),
            Days = 3,
            Reason = "Family event",
            Status = LeaveRequestStatus.Pending,
            ApproverId = manager.Id,
            CreatedAt = createdAt,
        };

        context.Employees.AddRange(grandBoss, manager, worker);
        context.LeaveTypes.Add(leaveType);
        context.LeaveRequests.Add(request);
        await context.SaveChangesAsync();

        return (grandBoss, manager, worker, request);
    }

    [Fact]
    public async Task Escalation_RequestPendingBeyondSla_ShouldEscalateToNextManager()
    {
        await using var context = CreateContext();
        var (grandBoss, _, _, request) = await SeedPendingRequestAsync(
            context, createdAt: UtcNow.AddHours(-49)); // beyond 48h SLA

        var notifier = new Mock<ILeaveNotifier>();
        var job = new LeaveEscalationJob(
            context, Clock().Object, notifier.Object,
            Options.Create(new LeaveSettings { EscalationSlaHours = 48 }),
            NullLogger<LeaveEscalationJob>.Instance);

        await job.RunAsync();

        var escalated = await context.LeaveRequests.SingleAsync();
        escalated.Status.Should().Be(LeaveRequestStatus.Escalated);
        escalated.ApproverId.Should().Be(grandBoss.Id);
        escalated.EscalatedAtUtc.Should().Be(UtcNow);

        (await context.AuditLogs.AnyAsync(a => a.Action == "Escalated")).Should().BeTrue();
        notifier.Verify(n => n.NotifyEscalatedAsync(
            It.IsAny<LeaveRequest>(), It.IsAny<Employee>(),
            It.Is<Employee>(e => e.Id == grandBoss.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Escalation_RequestWithinSla_ShouldNotEscalate()
    {
        await using var context = CreateContext();
        await SeedPendingRequestAsync(context, createdAt: UtcNow.AddHours(-24)); // within SLA

        var job = new LeaveEscalationJob(
            context, Clock().Object, Mock.Of<ILeaveNotifier>(),
            Options.Create(new LeaveSettings { EscalationSlaHours = 48 }),
            NullLogger<LeaveEscalationJob>.Instance);

        await job.RunAsync();

        (await context.LeaveRequests.SingleAsync()).Status.Should().Be(LeaveRequestStatus.Pending);
    }

    // -----------------------------------------------------------------------
    // Accrual & carry-forward
    // -----------------------------------------------------------------------

    private static async Task<(Employee Employee, LeaveType Type)> SeedPolicyAsync(
        Employee360DbContext context,
        AccrualFrequency frequency,
        decimal carryForwardMax = 5)
    {
        var employee = new Employee
        {
            EmployeeCode = "E1", FirstName = "Ada", LastName = "Okafor",
            Email = "ada@company.ng", Status = EmployeeStatus.Active,
        };
        var leaveType = new LeaveType { Name = "Annual Leave", Code = "ANN", IsPaid = true };
        var policy = new LeavePolicy
        {
            LeaveTypeId = leaveType.Id,
            AnnualEntitlement = 24,
            AccrualFrequency = frequency,
            CarryForwardMax = carryForwardMax,
        };

        context.Employees.Add(employee);
        context.LeaveTypes.Add(leaveType);
        context.LeavePolicies.Add(policy);
        await context.SaveChangesAsync();

        return (employee, leaveType);
    }

    [Fact]
    public async Task Accrual_AnnualPolicy_ShouldGrantFullEntitlement()
    {
        await using var context = CreateContext();
        var (employee, type) = await SeedPolicyAsync(context, AccrualFrequency.Annual);

        var job = new LeaveAccrualJob(
            context, Clock(today: new DateOnly(2026, 7, 20)).Object,
            NullLogger<LeaveAccrualJob>.Instance);

        await job.RunAsync();

        var balance = await context.LeaveBalances.SingleAsync();
        balance.EmployeeId.Should().Be(employee.Id);
        balance.LeaveTypeId.Should().Be(type.Id);
        balance.Entitled.Should().Be(24);
    }

    [Fact]
    public async Task Accrual_MonthlyPolicy_ShouldGrantProRataToDate()
    {
        await using var context = CreateContext();
        await SeedPolicyAsync(context, AccrualFrequency.Monthly);

        var job = new LeaveAccrualJob(
            context, Clock(today: new DateOnly(2026, 7, 20)).Object,
            NullLogger<LeaveAccrualJob>.Instance);

        await job.RunAsync();

        // 24 / 12 * 7 months = 14.
        (await context.LeaveBalances.SingleAsync()).Entitled.Should().Be(14);
    }

    [Fact]
    public async Task Accrual_ShouldApplyCarryForward_CappedAtPolicyMax()
    {
        await using var context = CreateContext();
        var (employee, type) = await SeedPolicyAsync(context, AccrualFrequency.Annual, carryForwardMax: 5);

        // Previous year: 20 entitled, 10 used -> 10 unused, but cap is 5.
        context.LeaveBalances.Add(new LeaveBalance
        {
            EmployeeId = employee.Id,
            LeaveTypeId = type.Id,
            Year = 2025,
            Entitled = 20,
            Used = 10,
        });
        await context.SaveChangesAsync();

        var job = new LeaveAccrualJob(
            context, Clock(today: new DateOnly(2026, 1, 5)).Object,
            NullLogger<LeaveAccrualJob>.Instance);

        await job.RunAsync();

        var balance = await context.LeaveBalances.SingleAsync(b => b.Year == 2026);
        balance.CarriedForward.Should().Be(5); // capped
        balance.Entitled.Should().Be(24);
    }

    [Fact]
    public async Task Accrual_RunTwice_ShouldBeIdempotent()
    {
        await using var context = CreateContext();
        await SeedPolicyAsync(context, AccrualFrequency.Monthly);

        var job = new LeaveAccrualJob(
            context, Clock(today: new DateOnly(2026, 7, 20)).Object,
            NullLogger<LeaveAccrualJob>.Instance);

        await job.RunAsync();
        await job.RunAsync();

        (await context.LeaveBalances.CountAsync()).Should().Be(1);
        (await context.LeaveBalances.SingleAsync()).Entitled.Should().Be(14);
    }
}
