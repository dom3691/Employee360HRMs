using Employee360.Application.Common.Services;
using Employee360.Application.Features.Notifications;
using Employee360.Domain.Entities;
using Employee360.Domain.Interfaces;
using Employee360.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Employee360.UnitTests.Application.SelfService;

/// <summary>Tests for the notification service and slices (ownership enforced).</summary>
public class NotificationTests
{
    private static readonly DateTime UtcNow = new(2026, 7, 20, 9, 0, 0, DateTimeKind.Utc);

    private static Employee360DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"notif-{Guid.NewGuid()}")
            .Options;

        return new Employee360DbContext(options);
    }

    private static Mock<ICurrentUserService> UserWithId(Guid userId)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.SetupGet(s => s.UserId).Returns(userId);
        return mock;
    }

    private static async Task<User> SeedUserAsync(
        Employee360DbContext context, Guid? employeeId = null)
    {
        var user = new User
        {
            Email = $"{Guid.NewGuid():N}@company.ng",
            PasswordHash = "x",
            EmployeeId = employeeId,
            IsActive = true,
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task NotifyEmployee_WithLinkedAccount_ShouldCreateNotification()
    {
        await using var context = CreateContext();
        var employee = new Employee
        {
            EmployeeCode = "EMP-00001", FirstName = "Ada", LastName = "Okafor",
            Email = "ada@company.ng",
        };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        var user = await SeedUserAsync(context, employee.Id);

        var service = new NotificationService(context, NullLogger<NotificationService>.Instance);

        await service.NotifyEmployeeAsync(employee.Id, "Test", "Message body", "/leave");

        var notification = await context.Notifications.SingleAsync();
        notification.UserId.Should().Be(user.Id);
        notification.Title.Should().Be("Test");
        notification.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task NotifyEmployee_WithoutAccount_ShouldBeNoOp()
    {
        await using var context = CreateContext();
        var service = new NotificationService(context, NullLogger<NotificationService>.Instance);

        await service.NotifyEmployeeAsync(Guid.NewGuid(), "Test", "Message");

        (await context.Notifications.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task GetMyNotifications_ShouldReturnOnlyOwnRows_NewestFirst()
    {
        await using var context = CreateContext();
        var me = await SeedUserAsync(context);
        var other = await SeedUserAsync(context);
        context.Notifications.AddRange(
            new Notification { UserId = me.Id, Title = "Old", Message = "m", CreatedAt = UtcNow.AddHours(-2) },
            new Notification { UserId = me.Id, Title = "New", Message = "m", CreatedAt = UtcNow },
            new Notification { UserId = other.Id, Title = "NotMine", Message = "m", CreatedAt = UtcNow });
        await context.SaveChangesAsync();

        var handler = new GetMyNotificationsHandler(context, UserWithId(me.Id).Object);

        var result = await handler.Handle(new GetMyNotificationsQuery(), default);

        result.Value.Should().HaveCount(2);
        result.Value[0].Title.Should().Be("New");
        result.Value.Select(n => n.Title).Should().NotContain("NotMine");
    }

    [Fact]
    public async Task GetMyNotifications_UnreadOnly_ShouldFilter()
    {
        await using var context = CreateContext();
        var me = await SeedUserAsync(context);
        context.Notifications.AddRange(
            new Notification { UserId = me.Id, Title = "Unread", Message = "m", IsRead = false },
            new Notification { UserId = me.Id, Title = "Read", Message = "m", IsRead = true });
        await context.SaveChangesAsync();

        var handler = new GetMyNotificationsHandler(context, UserWithId(me.Id).Object);

        var result = await handler.Handle(new GetMyNotificationsQuery(UnreadOnly: true), default);

        result.Value.Should().ContainSingle().Which.Title.Should().Be("Unread");
    }

    [Fact]
    public async Task MarkRead_ShouldStampReadState()
    {
        await using var context = CreateContext();
        var me = await SeedUserAsync(context);
        var notification = new Notification { UserId = me.Id, Title = "T", Message = "m" };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(UtcNow);
        var handler = new MarkNotificationReadHandler(context, UserWithId(me.Id).Object, clock.Object);

        var result = await handler.Handle(new MarkNotificationReadCommand(notification.Id), default);

        result.IsSuccess.Should().BeTrue();
        var stored = await context.Notifications.SingleAsync();
        stored.IsRead.Should().BeTrue();
        stored.ReadAtUtc.Should().Be(UtcNow);
    }

    [Fact]
    public async Task MarkRead_OnAnotherUsersNotification_ShouldFail()
    {
        await using var context = CreateContext();
        var owner = await SeedUserAsync(context);
        var attacker = await SeedUserAsync(context);
        var notification = new Notification { UserId = owner.Id, Title = "T", Message = "m" };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(UtcNow);
        var handler = new MarkNotificationReadHandler(context, UserWithId(attacker.Id).Object, clock.Object);

        var result = await handler.Handle(new MarkNotificationReadCommand(notification.Id), default);

        result.IsFailure.Should().BeTrue();
        (await context.Notifications.SingleAsync()).IsRead.Should().BeFalse();
    }
}
