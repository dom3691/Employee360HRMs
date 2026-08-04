using Employee360.Application.Features.Performance;
using Employee360.Application.Features.Performance.Feedback;
using Employee360.Application.Features.Performance.Reviews;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using Employee360.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Employee360.UnitTests.Application.Performance;

public class PerformanceWorkflowTests
{
    [Theory]
    [InlineData(ReviewCycleStatus.Draft, ReviewCycleStatus.Active, true)]
    [InlineData(ReviewCycleStatus.Active, ReviewCycleStatus.Closed, true)]
    [InlineData(ReviewCycleStatus.Draft, ReviewCycleStatus.Closed, false)]
    [InlineData(ReviewCycleStatus.Closed, ReviewCycleStatus.Active, false)]
    public void ReviewCycleTransition_ValidatesStates(
        ReviewCycleStatus from,
        ReviewCycleStatus to,
        bool shouldSucceed)
    {
        ReviewCycleWorkflow.EnsureTransition(from, to).IsSuccess.Should().Be(shouldSucceed);
    }

    [Fact]
    public void ReviewCycleWeights_MustSumTo100()
    {
        ReviewCycleWorkflow.EnsureWeightsSumTo100(70, 15, 15, 0).IsSuccess.Should().BeTrue();
        ReviewCycleWorkflow.EnsureWeightsSumTo100(70, 15, 10, 0).IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task PeerFeedbackAggregate_DoesNotExposeReviewerIdentity()
    {
        await using var context = CreateContext();

        var employee = new Employee
        {
            EmployeeCode = "E1", FirstName = "Review", LastName = "Target",
            Email = "target@co.ng", Status = EmployeeStatus.Active,
        };
        var reviewer = new Employee
        {
            EmployeeCode = "E2", FirstName = "Secret", LastName = "Reviewer",
            Email = "secret@co.ng", Status = EmployeeStatus.Active,
        };

        var cycle = new ReviewCycle
        {
            Name = "2026 Annual", Type = ReviewCycleType.Annual,
            StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31),
            Status = ReviewCycleStatus.Active,
        };

        var review = new PerformanceReview
        {
            ReviewCycleId = cycle.Id,
            EmployeeId = employee.Id,
        };

        context.Employees.AddRange(employee, reviewer);
        context.ReviewCycles.Add(cycle);
        context.PerformanceReviews.Add(review);
        await context.SaveChangesAsync();

        context.PeerFeedbacks.Add(new PeerFeedback
        {
            PerformanceReviewId = review.Id,
            ReviewerEmployeeId = reviewer.Id,
            Rating = 4.5m,
            Comments = "Great collaborator",
        });
        await context.SaveChangesAsync();

        var handler = new GetPeerFeedbackAggregateHandler(context);
        var result = await handler.Handle(
            new GetPeerFeedbackAggregateQuery(review.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ResponseCount.Should().Be(1);
        result.Value.AverageRating.Should().Be(4.5m);
        result.Value.SampleComments.Should().Contain("Great collaborator");

        var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
        json.Should().NotContain(reviewer.Id.ToString());
        json.Should().NotContain("ReviewerEmployeeId");
        json.Should().NotContain("Secret");
    }

    [Fact]
    public async Task SelfAssessment_BlockedWhenCycleNotActive()
    {
        await using var context = CreateContext();

        var employee = new Employee
        {
            EmployeeCode = "E1", FirstName = "Ada", LastName = "Okafor",
            Email = "ada@co.ng", Status = EmployeeStatus.Active,
        };

        var cycle = new ReviewCycle
        {
            Name = "Draft Cycle", Type = ReviewCycleType.Annual,
            StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31),
            Status = ReviewCycleStatus.Draft,
        };

        var review = new PerformanceReview
        {
            ReviewCycleId = cycle.Id,
            EmployeeId = employee.Id,
        };

        context.Employees.Add(employee);
        context.ReviewCycles.Add(cycle);
        context.PerformanceReviews.Add(review);
        await context.SaveChangesAsync();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.EmployeeId).Returns(employee.Id);

        var handler = new SubmitSelfAssessmentHandler(
            context, currentUser.Object, Mock.Of<IDateTimeProvider>());

        var result = await handler.Handle(
            new SubmitSelfAssessmentCommand(review.Id, 4m, "Good year", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("active");
    }

    private static Employee360DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"perf-{Guid.NewGuid()}")
            .Options;

        return new Employee360DbContext(options);
    }
}
