using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Application.Common.Services;
using Employee360.Application.Features.Attendance;
using Employee360.Domain.Constants;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using Employee360.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace Employee360.UnitTests.Application.Attendance;

/// <summary>Tests for approved leave → attendance integration (FR-ATT-008).</summary>
public class AttendanceLeaveIntegrationTests
{
    [Fact]
    public async Task SyncApprovedLeave_CreatesOnLeaveRecordsForWorkingDays()
    {
        await using var context = CreateContext();

        var employee = new Employee
        {
            EmployeeCode = "EMP-00010",
            FirstName = "Ada",
            LastName = "Okafor",
            Email = "ada@company.ng",
            Status = EmployeeStatus.Active,
        };

        var leaveType = new LeaveType { Name = "Annual", Code = "ANN", IsPaid = true };

        var request = new LeaveRequest
        {
            EmployeeId = employee.Id,
            Employee = employee,
            LeaveTypeId = leaveType.Id,
            LeaveType = leaveType,
            StartDate = new DateOnly(2026, 8, 3), // Mon
            EndDate = new DateOnly(2026, 8, 5),   // Wed
            Days = 3,
            Reason = "Travel",
            Status = LeaveRequestStatus.Approved,
        };

        context.Employees.Add(employee);
        context.LeaveTypes.Add(leaveType);
        context.LeaveRequests.Add(request);
        await context.SaveChangesAsync();

        var service = new AttendanceIntegrationService(
            context,
            Options.Create(new AttendanceSettings()));

        await service.SyncApprovedLeaveAsync(request);

        var records = await context.AttendanceRecords
            .Where(r => r.EmployeeId == employee.Id)
            .OrderBy(r => r.Date)
            .ToListAsync();

        records.Should().HaveCount(3);
        records.Should().OnlyContain(r => r.Status == AttendanceStatus.OnLeave);
        records.Select(r => r.Date).Should().ContainInOrder(
            new DateOnly(2026, 8, 3),
            new DateOnly(2026, 8, 4),
            new DateOnly(2026, 8, 5));
    }

    [Fact]
    public async Task ManualCorrection_WritesAuditLogEntry()
    {
        await using var context = CreateContext();

        var manager = new Employee
        {
            EmployeeCode = "EMP-00001",
            FirstName = "Musa",
            LastName = "Bello",
            Email = "musa@company.ng",
            Status = EmployeeStatus.Active,
        };

        var employee = new Employee
        {
            EmployeeCode = "EMP-00002",
            FirstName = "Ada",
            LastName = "Okafor",
            Email = "ada@company.ng",
            ManagerId = manager.Id,
            Status = EmployeeStatus.Active,
        };

        context.Employees.AddRange(manager, employee);
        await context.SaveChangesAsync();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.EmployeeId).Returns(manager.Id);
        currentUser.SetupGet(c => c.UserId).Returns(Guid.NewGuid());
        currentUser.Setup(c => c.IsInRole(RoleNames.HRAdmin)).Returns(false);
        currentUser.Setup(c => c.IsInRole(RoleNames.HRManager)).Returns(false);

        var managerScope = new Mock<IManagerScopeService>();
        managerScope
            .Setup(m => m.IsManagerOfAsync(manager.Id, employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(new DateTime(2026, 8, 4, 10, 0, 0, DateTimeKind.Utc));

        var handler = new ManualAttendanceCorrectionHandler(
            context,
            currentUser.Object,
            managerScope.Object,
            new AttendanceIntegrationService(context, Options.Create(new AttendanceSettings())),
            clock.Object);

        var date = new DateOnly(2026, 8, 4);
        var clockIn = new DateTime(2026, 8, 4, 8, 0, 0, DateTimeKind.Utc);

        var result = await handler.Handle(
            new ManualAttendanceCorrectionCommand(
                employee.Id,
                date,
                clockIn,
                ClockOut: null,
                Status: null,
                Reason: "Forgot to clock in — verified with security"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var audit = await context.AuditLogs
            .SingleAsync(a => a.EntityName == nameof(AttendanceRecord));

        audit.Action.Should().Be("ManualCorrection");
        audit.NewValues.Should().Contain("Forgot to clock in");
    }

    private static Employee360DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"attendance-{Guid.NewGuid()}")
            .Options;

        return new Employee360DbContext(options);
    }
}
