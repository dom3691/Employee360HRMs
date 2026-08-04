using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Application.Common.Services;
using Employee360.Application.Features.Employees.CreateEmployee;
using Employee360.Application.Features.Recruitment.Candidates;
using Employee360.Application.Features.Recruitment.Onboarding;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using Employee360.Infrastructure.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Employee360.UnitTests.Application.Recruitment;

public class RecruitmentIntegrationTests
{
    [Fact]
    public async Task ConvertCandidateToEmployee_HiredCandidate_CreatesEmployeeAndLinks()
    {
        await using var context = CreateContext();
        var (candidateId, _) = await SeedHiredCandidateAsync(context);

        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns("cipher");

        var createHandler = new CreateEmployeeHandler(
            context,
            encryption.Object,
            Options.Create(new EmployeeSettings()));

        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<CreateEmployeeCommand>(), It.IsAny<CancellationToken>()))
            .Returns(async (CreateEmployeeCommand cmd, CancellationToken ct) =>
                await createHandler.Handle(cmd, ct));

        var handler = new ConvertCandidateToEmployeeHandler(context, mediator.Object);

        var result = await handler.Handle(
            new ConvertCandidateToEmployeeCommand(candidateId, null, null, new DateOnly(2026, 9, 1)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var candidate = await context.Candidates.SingleAsync();
        candidate.EmployeeId.Should().NotBeNull();

        var employee = await context.Employees.SingleAsync(e => e.Id == candidate.EmployeeId);
        employee.Email.Should().Be("ada@example.ng");
        employee.FirstName.Should().Be("Ada");
        employee.LastName.Should().Be("Okafor");
    }

    [Fact]
    public async Task CompleteOnboardingTask_SetsCompletedTimestamp()
    {
        await using var context = CreateContext();
        var employee = new Employee
        {
            EmployeeCode = "E1",
            FirstName = "New",
            LastName = "Hire",
            Email = "new@co.ng",
            Status = EmployeeStatus.Active,
        };
        var assignee = new Employee
        {
            EmployeeCode = "E2",
            FirstName = "HR",
            LastName = "Admin",
            Email = "hr@co.ng",
            Status = EmployeeStatus.Active,
        };

        context.Employees.AddRange(employee, assignee);
        await context.SaveChangesAsync();

        var assignHandler = new AssignOnboardingTaskHandler(
            context,
            Mock.Of<IEmailService>(),
            Mock.Of<IEmailTemplateService>());

        var taskId = (await assignHandler.Handle(
            new AssignOnboardingTaskCommand(
                employee.Id, "Complete IT setup", assignee.Id, new DateOnly(2026, 9, 15)),
            CancellationToken.None)).Value;

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(new DateTime(2026, 9, 10, 10, 0, 0, DateTimeKind.Utc));

        var completeHandler = new CompleteOnboardingTaskHandler(context, clock.Object);
        (await completeHandler.Handle(new CompleteOnboardingTaskCommand(taskId), CancellationToken.None))
            .IsSuccess.Should().BeTrue();

        var task = await context.OnboardingTasks.SingleAsync();
        task.IsCompleted.Should().BeTrue();
        task.CompletedAtUtc.Should().Be(clock.Object.UtcNow);
    }

    [Fact]
    public async Task TransitionStage_InvalidJump_ReturnsFailure()
    {
        await using var context = CreateContext();
        var posting = new JobPosting { Title = "Dev", Description = "Role", Status = JobPostingStatus.Published };
        var candidate = new Candidate
        {
            JobPostingId = posting.Id,
            Name = "Test User",
            Email = "test@co.ng",
            Stage = CandidateStage.Applied,
        };

        context.JobPostings.Add(posting);
        context.Candidates.Add(candidate);
        await context.SaveChangesAsync();

        var handler = new TransitionCandidateStageHandler(
            context,
            Mock.Of<IRecruitmentNotifier>());

        var result = await handler.Handle(
            new TransitionCandidateStageCommand(candidate.Id, CandidateStage.Hired),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    private static async Task<(Guid CandidateId, Guid JobPostingId)> SeedHiredCandidateAsync(
        Employee360DbContext context)
    {
        var dept = new Department { Name = "Engineering", Code = "ENG" };
        var posting = new JobPosting
        {
            Title = "Software Engineer",
            Description = "Build HR software",
            DepartmentId = dept.Id,
            Status = JobPostingStatus.Published,
        };

        context.Departments.Add(dept);
        context.JobPostings.Add(posting);
        await context.SaveChangesAsync();

        var candidate = new Candidate
        {
            JobPostingId = posting.Id,
            Name = "Ada Okafor",
            Email = "ada@example.ng",
            Phone = "08012345678",
            Stage = CandidateStage.Hired,
        };

        context.Candidates.Add(candidate);
        await context.SaveChangesAsync();

        return (candidate.Id, posting.Id);
    }

    private static Employee360DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"recruitment-{Guid.NewGuid()}")
            .Options;

        return new Employee360DbContext(options);
    }
}
