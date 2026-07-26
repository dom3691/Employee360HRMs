using Employee360.Application.Features.SelfService.GetEssDashboard;
using Employee360.Application.Features.SelfService.GetMssDashboard;
using Employee360.Application.Features.SelfService.GetMyTeam;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using Employee360.Infrastructure.Identity;
using Employee360.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Employee360.UnitTests.Application.SelfService;

/// <summary>
/// Tests for ESS/MSS dashboards and team list: aggregation correctness and
/// manager-scope enforcement (FR-ESS-001, FR-MSS-001/002).
/// </summary>
public class SelfServiceDashboardTests
{
    private static readonly DateOnly Today = new(2026, 7, 20); // Monday
    private static readonly DateTime UtcNow = new(2026, 7, 20, 9, 0, 0, DateTimeKind.Utc);

    private readonly Employee360DbContext _context;
    private readonly Employee _manager;
    private readonly Employee _reportA;
    private readonly Employee _reportB;      // reports to reportA (indirect for manager)
    private readonly Employee _outsider;     // different team entirely
    private readonly User _managerUser;
    private readonly LeaveType _annual;

    public SelfServiceDashboardTests()
    {
        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"selfservice-{Guid.NewGuid()}")
            .Options;
        _context = new Employee360DbContext(options);

        _manager = NewEmployee("EMP-00001", "Musa", "Bello", "musa@company.ng", null);
        _reportA = NewEmployee("EMP-00002", "Ada", "Okafor", "ada@company.ng", _manager.Id,
            dateOfBirth: Today.AddDays(10).AddYears(-30)); // birthday in 10 days
        _reportB = NewEmployee("EMP-00003", "Bola", "Adeyemi", "bola@company.ng", _reportA.Id,
            dateOfBirth: Today.AddDays(100).AddYears(-25)); // outside 30-day window
        _outsider = NewEmployee("EMP-00099", "Out", "Sider", "out@company.ng", null);

        _managerUser = new User
        {
            Email = "musa@company.ng",
            PasswordHash = "x",
            EmployeeId = _manager.Id,
            IsActive = true,
        };

        _annual = new LeaveType { Name = "Annual Leave", Code = "ANN", IsPaid = true };

        _context.Employees.AddRange(_manager, _reportA, _reportB, _outsider);
        _context.Users.Add(_managerUser);
        _context.LeaveTypes.Add(_annual);
        _context.SaveChanges();
    }

    private static Employee NewEmployee(
        string code, string first, string last, string email, Guid? managerId,
        DateOnly? dateOfBirth = null) => new()
    {
        EmployeeCode = code,
        FirstName = first,
        LastName = last,
        Email = email,
        ManagerId = managerId,
        Status = EmployeeStatus.Active,
        DateOfBirth = dateOfBirth,
    };

    private Mock<ICurrentUserService> UserFor(Employee employee, Guid? userId = null)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.SetupGet(s => s.EmployeeId).Returns(employee.Id);
        mock.SetupGet(s => s.UserId).Returns(userId);
        return mock;
    }

    private static Mock<IDateTimeProvider> Clock()
    {
        var mock = new Mock<IDateTimeProvider>();
        mock.SetupGet(c => c.UtcNow).Returns(UtcNow);
        mock.SetupGet(c => c.TodayWat).Returns(Today);
        return mock;
    }

    // -----------------------------------------------------------------------
    // ESS dashboard
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EssDashboard_ShouldAggregateBalancesPendingAndNotifications()
    {
        _context.LeaveBalances.Add(new LeaveBalance
        {
            EmployeeId = _reportA.Id,
            LeaveTypeId = _annual.Id,
            Year = 2026,
            Entitled = 20,
            Used = 5,
            Pending = 2,
        });
        _context.LeaveRequests.Add(new LeaveRequest
        {
            EmployeeId = _reportA.Id,
            LeaveTypeId = _annual.Id,
            StartDate = new DateOnly(2026, 8, 3),
            EndDate = new DateOnly(2026, 8, 4),
            Days = 2,
            Reason = "Trip",
            Status = LeaveRequestStatus.Pending,
            ApproverId = _manager.Id,
        });

        var reportAUser = new User
        {
            Email = "ada@company.ng", PasswordHash = "x",
            EmployeeId = _reportA.Id, IsActive = true,
        };
        _context.Users.Add(reportAUser);
        _context.Notifications.AddRange(
            new Notification { UserId = reportAUser.Id, Title = "N1", Message = "m", IsRead = false },
            new Notification { UserId = reportAUser.Id, Title = "N2", Message = "m", IsRead = true });
        await _context.SaveChangesAsync();

        var handler = new GetEssDashboardHandler(
            _context, UserFor(_reportA, reportAUser.Id).Object, Clock().Object);

        var result = await handler.Handle(new GetEssDashboardQuery(), default);

        result.IsSuccess.Should().BeTrue();
        var dashboard = result.Value;
        dashboard.EmployeeName.Should().Be("Ada Okafor");
        dashboard.LeaveBalances.Should().ContainSingle()
            .Which.Available.Should().Be(13);
        dashboard.PendingRequestCount.Should().Be(1);
        dashboard.UnreadNotificationCount.Should().Be(1);
        dashboard.RecentNotifications.Should().HaveCount(2);
        dashboard.QuickActions.Should().Contain("apply-leave");
    }

    // -----------------------------------------------------------------------
    // MSS dashboard
    // -----------------------------------------------------------------------

    [Fact]
    public async Task MssDashboard_ShouldAggregateTeamData()
    {
        // Pending approval from direct report; approved leave covering today for indirect report.
        _context.LeaveRequests.AddRange(
            new LeaveRequest
            {
                EmployeeId = _reportA.Id,
                LeaveTypeId = _annual.Id,
                StartDate = new DateOnly(2026, 8, 3),
                EndDate = new DateOnly(2026, 8, 4),
                Days = 2,
                Reason = "Trip",
                Status = LeaveRequestStatus.Pending,
                ApproverId = _manager.Id,
            },
            new LeaveRequest
            {
                EmployeeId = _reportB.Id,
                LeaveTypeId = _annual.Id,
                StartDate = Today.AddDays(-1),
                EndDate = Today.AddDays(1),
                Days = 3,
                Reason = "Rest",
                Status = LeaveRequestStatus.Approved,
                ApproverId = _reportA.Id,
            });
        await _context.SaveChangesAsync();

        var handler = new GetMssDashboardHandler(
            _context, UserFor(_manager).Object,
            new ManagerScopeService(_context), Clock().Object);

        var result = await handler.Handle(new GetMssDashboardQuery(), default);

        result.IsSuccess.Should().BeTrue();
        var dashboard = result.Value;

        dashboard.PendingApprovalCount.Should().Be(1);
        dashboard.PendingApprovals.Should().ContainSingle()
            .Which.EmployeeName.Should().Be("Ada Okafor");

        dashboard.TeamOnLeaveToday.Should().ContainSingle()
            .Which.EmployeeName.Should().Be("Bola Adeyemi");

        dashboard.DirectReportCount.Should().Be(1);   // Ada only
        dashboard.TotalTeamCount.Should().Be(2);      // Ada + Bola

        dashboard.UpcomingBirthdays.Should().ContainSingle()
            .Which.EmployeeName.Should().Be("Ada Okafor"); // Bola is outside 30 days
    }

    [Fact]
    public async Task MssDashboard_ShouldExcludeOtherTeams()
    {
        _context.LeaveRequests.Add(new LeaveRequest
        {
            EmployeeId = _outsider.Id,
            LeaveTypeId = _annual.Id,
            StartDate = Today,
            EndDate = Today,
            Days = 1,
            Reason = "Other team",
            Status = LeaveRequestStatus.Approved,
            ApproverId = null,
        });
        await _context.SaveChangesAsync();

        var handler = new GetMssDashboardHandler(
            _context, UserFor(_manager).Object,
            new ManagerScopeService(_context), Clock().Object);

        var result = await handler.Handle(new GetMssDashboardQuery(), default);

        result.Value.TeamOnLeaveToday.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // My team (manager scope)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task MyTeam_ShouldReturnOnlyReportingSubtree()
    {
        var handler = new GetMyTeamHandler(
            _context, UserFor(_manager).Object, new ManagerScopeService(_context));

        var result = await handler.Handle(new GetMyTeamQuery(), default);

        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Select(i => i.FullName)
            .Should().BeEquivalentTo(["Ada Okafor", "Bola Adeyemi"])
            .And.NotContain("Out Sider");
    }

    [Fact]
    public async Task MyTeam_ShouldApplySearchAndStatusFilter()
    {
        _reportB.Status = EmployeeStatus.Resigned;
        await _context.SaveChangesAsync();

        var handler = new GetMyTeamHandler(
            _context, UserFor(_manager).Object, new ManagerScopeService(_context));

        var active = await handler.Handle(new GetMyTeamQuery(Status: EmployeeStatus.Active), default);
        active.Value.Items.Should().ContainSingle().Which.FullName.Should().Be("Ada Okafor");

        var searched = await handler.Handle(new GetMyTeamQuery(Search: "bola"), default);
        searched.Value.Items.Should().ContainSingle().Which.FullName.Should().Be("Bola Adeyemi");
    }

    [Fact]
    public async Task MyTeam_ForNonManager_ShouldBeEmpty()
    {
        var handler = new GetMyTeamHandler(
            _context, UserFor(_reportB).Object, new ManagerScopeService(_context));

        var result = await handler.Handle(new GetMyTeamQuery(), default);

        result.Value.TotalCount.Should().Be(0);
    }
}
