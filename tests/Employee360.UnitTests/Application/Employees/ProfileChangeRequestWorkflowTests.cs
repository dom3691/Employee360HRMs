using Employee360.Application.Features.Employees.ReviewProfileChangeRequest;
using Employee360.Application.Features.Employees.SubmitProfileChangeRequest;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using Employee360.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Employee360.UnitTests.Application.Employees;

/// <summary>End-to-end tests for the profile change approval workflow (FR-EMP-011).</summary>
public class ProfileChangeRequestWorkflowTests
{
    private static readonly Guid ReviewerId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 7, 26, 12, 0, 0, DateTimeKind.Utc);

    private static Employee360DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"pcr-tests-{Guid.NewGuid()}")
            .Options;

        return new Employee360DbContext(options);
    }

    private static async Task<Employee> SeedEmployeeAsync(Employee360DbContext context)
    {
        var employee = new Employee
        {
            EmployeeCode = "EMP-00001",
            FirstName = "Ada",
            LastName = "Okafor",
            Email = "ada@company.ng",
            PhoneNumber = "+2348000000000",
            Status = EmployeeStatus.Active,
        };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        return employee;
    }

    private static Mock<ICurrentUserService> MockUser(Guid? employeeId, Guid? userId = null)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.SetupGet(s => s.EmployeeId).Returns(employeeId);
        mock.SetupGet(s => s.UserId).Returns(userId ?? Guid.NewGuid());
        return mock;
    }

    [Fact]
    public async Task SubmitThenApprove_ShouldApplyChangesToEmployee()
    {
        await using var context = CreateContext();
        var employee = await SeedEmployeeAsync(context);

        var submitHandler = new SubmitProfileChangeRequestHandler(
            context, MockUser(employee.Id).Object);

        var submitResult = await submitHandler.Handle(
            new SubmitProfileChangeRequestCommand(
                new Dictionary<string, string> { ["PhoneNumber"] = "+2348111111111" }),
            default);

        submitResult.IsSuccess.Should().BeTrue();

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(UtcNow);

        var reviewHandler = new ReviewProfileChangeRequestHandler(
            context, MockUser(null, ReviewerId).Object, clock.Object);

        var reviewResult = await reviewHandler.Handle(
            new ReviewProfileChangeRequestCommand(submitResult.Value, Approve: true, Comment: "Verified"),
            default);

        reviewResult.IsSuccess.Should().BeTrue();

        var updated = await context.Employees.SingleAsync(e => e.Id == employee.Id);
        updated.PhoneNumber.Should().Be("+2348111111111");

        var request = await context.ProfileChangeRequests.SingleAsync();
        request.Status.Should().Be(ProfileChangeRequestStatus.Approved);
        request.ReviewedBy.Should().Be(ReviewerId);
        request.ReviewedAtUtc.Should().Be(UtcNow);
    }

    [Fact]
    public async Task Reject_ShouldNotApplyChanges()
    {
        await using var context = CreateContext();
        var employee = await SeedEmployeeAsync(context);
        var originalPhone = employee.PhoneNumber;

        var submitHandler = new SubmitProfileChangeRequestHandler(
            context, MockUser(employee.Id).Object);
        var submitResult = await submitHandler.Handle(
            new SubmitProfileChangeRequestCommand(
                new Dictionary<string, string> { ["PhoneNumber"] = "+2348999999999" }),
            default);

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(UtcNow);
        var reviewHandler = new ReviewProfileChangeRequestHandler(
            context, MockUser(null, ReviewerId).Object, clock.Object);

        await reviewHandler.Handle(
            new ReviewProfileChangeRequestCommand(submitResult.Value, Approve: false, Comment: "Not verified"),
            default);

        (await context.Employees.SingleAsync()).PhoneNumber.Should().Be(originalPhone);
        (await context.ProfileChangeRequests.SingleAsync()).Status
            .Should().Be(ProfileChangeRequestStatus.Rejected);
    }

    [Fact]
    public async Task SecondPendingRequest_ShouldBeRejected()
    {
        await using var context = CreateContext();
        var employee = await SeedEmployeeAsync(context);
        var handler = new SubmitProfileChangeRequestHandler(context, MockUser(employee.Id).Object);
        var changes = new Dictionary<string, string> { ["Address"] = "New Address" };

        await handler.Handle(new SubmitProfileChangeRequestCommand(changes), default);
        var second = await handler.Handle(new SubmitProfileChangeRequestCommand(changes), default);

        second.IsFailure.Should().BeTrue();
        second.Error.Should().Contain("pending");
    }

    [Fact]
    public void Validator_ShouldRejectNonWhitelistedFields()
    {
        var validator = new SubmitProfileChangeRequestValidator();

        var result = validator.Validate(new SubmitProfileChangeRequestCommand(
            new Dictionary<string, string> { ["Email"] = "hacker@evil.com" }));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("cannot be changed"));
    }
}
