using Employee360.Application.Common.Services;
using Employee360.Application.Features.Leave.ApplyLeave;
using Employee360.Application.Features.Leave.ApproveLeave;
using Employee360.Application.Features.Leave.Common;
using Employee360.Application.Features.Leave.RejectLeave;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using Employee360.Infrastructure.Identity;
using Employee360.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Employee360.UnitTests.Application.Leave;

/// <summary>
/// End-to-end leave workflow tests: balance validation on apply, overlap
/// prevention, and balance movement on approve/reject (FR-LV-005..008).
/// </summary>
public class LeaveWorkflowTests
{
    private static readonly DateTime UtcNow = new(2026, 7, 20, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 7, 20); // Monday

    private readonly Employee360DbContext _context;
    private readonly Employee _manager;
    private readonly Employee _employee;
    private readonly LeaveType _annualLeave;
    private readonly Mock<ILeaveNotifier> _notifier = new();
    private readonly Mock<IAttendanceIntegrationService> _attendanceIntegration = new();
    private readonly Guid _managerUserId = Guid.NewGuid();

    public LeaveWorkflowTests()
    {
        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"leave-flow-{Guid.NewGuid()}")
            .Options;
        _context = new Employee360DbContext(options);

        _manager = new Employee
        {
            EmployeeCode = "EMP-00001",
            FirstName = "Musa",
            LastName = "Bello",
            Email = "musa@company.ng",
            Status = EmployeeStatus.Active,
        };
        _employee = new Employee
        {
            EmployeeCode = "EMP-00002",
            FirstName = "Ada",
            LastName = "Okafor",
            Email = "ada@company.ng",
            ManagerId = _manager.Id,
            Status = EmployeeStatus.Active,
            JoinDate = new DateOnly(2024, 1, 1),
        };
        _annualLeave = new LeaveType
        {
            Name = "Annual Leave",
            Code = "ANN",
            IsPaid = true,
            Policy = null!,
        };
        _annualLeave.Policy = new LeavePolicy
        {
            LeaveTypeId = _annualLeave.Id,
            AnnualEntitlement = 20,
            AccrualFrequency = AccrualFrequency.Annual,
            CarryForwardMax = 5,
        };

        _context.Employees.AddRange(_manager, _employee);
        _context.LeaveTypes.Add(_annualLeave);
        _context.LeaveBalances.Add(new LeaveBalance
        {
            EmployeeId = _employee.Id,
            LeaveTypeId = _annualLeave.Id,
            Year = 2026,
            Entitled = 20,
            Used = 15,
        });
        _context.SaveChanges();
    }

    private Mock<ICurrentUserService> UserAs(Guid? employeeId, Guid? userId = null, params string[] roles)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.SetupGet(s => s.EmployeeId).Returns(employeeId);
        mock.SetupGet(s => s.UserId).Returns(userId ?? Guid.NewGuid());
        mock.Setup(s => s.IsInRole(It.IsAny<string>())).Returns<string>(roles.Contains);
        return mock;
    }

    private Mock<IDateTimeProvider> Clock()
    {
        var mock = new Mock<IDateTimeProvider>();
        mock.SetupGet(c => c.UtcNow).Returns(UtcNow);
        mock.SetupGet(c => c.TodayWat).Returns(Today);
        return mock;
    }

    private ApplyLeaveHandler CreateApplyHandler(Guid employeeId) => new(
        _context,
        UserAs(employeeId).Object,
        new WorkingDaysCalculator(_context),
        Clock().Object,
        _notifier.Object);

    private LeaveDecisionService CreateDecisionService(Guid approverEmployeeId) => new(
        _context,
        UserAs(approverEmployeeId, _managerUserId).Object,
        new ManagerScopeService(_context),
        Clock().Object,
        _notifier.Object,
        _attendanceIntegration.Object);

    [Fact]
    public async Task Apply_WithSufficientBalance_ShouldReservePendingDays()
    {
        var handler = CreateApplyHandler(_employee.Id);

        // Mon 3 Aug – Wed 5 Aug 2026 = 3 working days; 5 available.
        var result = await handler.Handle(
            new ApplyLeaveCommand(_annualLeave.Id, new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 5), "Family event"),
            default);

        result.IsSuccess.Should().BeTrue();

        var balance = await _context.LeaveBalances.SingleAsync();
        balance.Pending.Should().Be(3);
        balance.Available.Should().Be(2);

        _notifier.Verify(n => n.NotifySubmittedAsync(
            It.IsAny<LeaveRequest>(), It.IsAny<Employee>(), It.IsAny<Employee>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Apply_ExceedingBalance_ShouldFail()
    {
        var handler = CreateApplyHandler(_employee.Id);

        // Mon 3 Aug – Mon 10 Aug 2026 = 6 working days; only 5 available.
        var result = await handler.Handle(
            new ApplyLeaveCommand(_annualLeave.Id, new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 10), "Long trip"),
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Insufficient balance");
    }

    [Fact]
    public async Task Apply_OverlappingExistingRequest_ShouldFail()
    {
        var handler = CreateApplyHandler(_employee.Id);
        await handler.Handle(
            new ApplyLeaveCommand(_annualLeave.Id, new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 4), "First"),
            default);

        var overlap = await handler.Handle(
            new ApplyLeaveCommand(_annualLeave.Id, new DateOnly(2026, 8, 4), new DateOnly(2026, 8, 5), "Second"),
            default);

        overlap.IsFailure.Should().BeTrue();
        overlap.Error.Should().Contain("overlapping");
    }

    [Fact]
    public async Task Apply_WithoutManager_ShouldFail()
    {
        var handler = CreateApplyHandler(_manager.Id); // manager has no manager

        var result = await handler.Handle(
            new ApplyLeaveCommand(_annualLeave.Id, new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 4), "Trip"),
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("no line manager");
    }

    [Fact]
    public async Task Approve_ShouldMovePendingToUsed_AndRecordStep()
    {
        var applyHandler = CreateApplyHandler(_employee.Id);
        var applied = await applyHandler.Handle(
            new ApplyLeaveCommand(_annualLeave.Id, new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 5), "Family event"),
            default);

        var approveHandler = new ApproveLeaveHandler(CreateDecisionService(_manager.Id));
        var result = await approveHandler.Handle(new ApproveLeaveCommand(applied.Value, "Enjoy"), default);

        result.IsSuccess.Should().BeTrue();

        var balance = await _context.LeaveBalances.SingleAsync();
        balance.Pending.Should().Be(0);
        balance.Used.Should().Be(18);

        var request = await _context.LeaveRequests.SingleAsync();
        request.Status.Should().Be(LeaveRequestStatus.Approved);

        var step = await _context.LeaveApprovals.SingleAsync();
        step.Action.Should().Be(ApprovalAction.Approve);
        step.ApproverUserId.Should().Be(_managerUserId);
    }

    [Fact]
    public async Task Reject_ShouldReleasePendingDays()
    {
        var applyHandler = CreateApplyHandler(_employee.Id);
        var applied = await applyHandler.Handle(
            new ApplyLeaveCommand(_annualLeave.Id, new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 5), "Family event"),
            default);

        var rejectHandler = new RejectLeaveHandler(CreateDecisionService(_manager.Id));
        var result = await rejectHandler.Handle(
            new RejectLeaveCommand(applied.Value, "Coverage gap that week"), default);

        result.IsSuccess.Should().BeTrue();

        var balance = await _context.LeaveBalances.SingleAsync();
        balance.Pending.Should().Be(0);
        balance.Used.Should().Be(15); // unchanged

        (await _context.LeaveRequests.SingleAsync()).Status.Should().Be(LeaveRequestStatus.Rejected);
    }

    [Fact]
    public async Task Decide_ByUnrelatedEmployee_ShouldBeDenied()
    {
        var stranger = new Employee
        {
            EmployeeCode = "EMP-00099",
            FirstName = "Random",
            LastName = "Person",
            Email = "random@company.ng",
        };
        _context.Employees.Add(stranger);
        await _context.SaveChangesAsync();

        var applyHandler = CreateApplyHandler(_employee.Id);
        var applied = await applyHandler.Handle(
            new ApplyLeaveCommand(_annualLeave.Id, new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 5), "Family event"),
            default);

        var approveHandler = new ApproveLeaveHandler(CreateDecisionService(stranger.Id));
        var result = await approveHandler.Handle(new ApproveLeaveCommand(applied.Value, null), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not authorized");
    }
}
