using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Constants;
using Employee360.Domain.Entities;
using Employee360.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Recruitment.Onboarding;

public sealed record OnboardingTaskDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string TaskName,
    Guid AssignedToEmployeeId,
    string AssignedToName,
    DateOnly DueDate,
    bool IsCompleted,
    DateTime? CompletedAtUtc);

public sealed record AssignOnboardingTaskCommand(
    Guid EmployeeId,
    string TaskName,
    Guid AssignedToEmployeeId,
    DateOnly DueDate) : IRequest<Result<Guid>>;

public sealed class AssignOnboardingTaskValidator : AbstractValidator<AssignOnboardingTaskCommand>
{
    public AssignOnboardingTaskValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.TaskName).NotEmpty().MaximumLength(256);
        RuleFor(c => c.AssignedToEmployeeId).NotEmpty();
    }
}

public sealed class AssignOnboardingTaskHandler : IRequestHandler<AssignOnboardingTaskCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplates;

    public AssignOnboardingTaskHandler(
        IApplicationDbContext context,
        IEmailService emailService,
        IEmailTemplateService emailTemplates)
    {
        _context = context;
        _emailService = emailService;
        _emailTemplates = emailTemplates;
    }

    public async Task<Result<Guid>> Handle(
        AssignOnboardingTaskCommand request,
        CancellationToken cancellationToken)
    {
        if (!await _context.Employees.AnyAsync(e => e.Id == request.EmployeeId, cancellationToken))
        {
            return Result.Failure<Guid>("Employee not found.");
        }

        var assignee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.AssignedToEmployeeId, cancellationToken);

        if (assignee is null)
        {
            return Result.Failure<Guid>("Assigned employee not found.");
        }

        var task = new OnboardingTask
        {
            EmployeeId = request.EmployeeId,
            TaskName = request.TaskName.Trim(),
            AssignedToEmployeeId = request.AssignedToEmployeeId,
            DueDate = request.DueDate,
        };

        _context.OnboardingTasks.Add(task);
        await _context.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(assignee.Email))
        {
            var tokens = new Dictionary<string, string>
            {
                ["TaskName"] = task.TaskName,
                ["DueDate"] = task.DueDate.ToString("dd MMMM yyyy"),
            };

            var rendered = await _emailTemplates.RenderAsync(
                EmailTemplateCodes.OnboardingTaskAssigned, tokens, cancellationToken);

            await _emailService.SendAsync(
                assignee.Email, rendered.Subject, rendered.BodyHtml, cancellationToken);
        }

        return Result.Success(task.Id);
    }
}

public sealed record CompleteOnboardingTaskCommand(Guid TaskId) : IRequest<Result>;

public sealed class CompleteOnboardingTaskHandler : IRequestHandler<CompleteOnboardingTaskCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _clock;

    public CompleteOnboardingTaskHandler(IApplicationDbContext context, IDateTimeProvider clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<Result> Handle(CompleteOnboardingTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _context.OnboardingTasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task is null)
        {
            return Result.Failure("Onboarding task not found.");
        }

        if (task.IsCompleted)
        {
            return Result.Success();
        }

        task.IsCompleted = true;
        task.CompletedAtUtc = _clock.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record GetOnboardingTasksQuery(Guid? EmployeeId) : IRequest<Result<IReadOnlyList<OnboardingTaskDto>>>;

public sealed class GetOnboardingTasksHandler : IRequestHandler<GetOnboardingTasksQuery, Result<IReadOnlyList<OnboardingTaskDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetOnboardingTasksHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<IReadOnlyList<OnboardingTaskDto>>> Handle(
        GetOnboardingTasksQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.OnboardingTasks.AsNoTracking();

        if (request.EmployeeId.HasValue)
        {
            query = query.Where(t => t.EmployeeId == request.EmployeeId.Value);
        }

        var items = await query
            .OrderBy(t => t.DueDate)
            .Select(t => new OnboardingTaskDto(
                t.Id,
                t.EmployeeId,
                t.Employee!.FirstName + " " + t.Employee.LastName,
                t.TaskName,
                t.AssignedToEmployeeId,
                t.AssignedToEmployee!.FirstName + " " + t.AssignedToEmployee.LastName,
                t.DueDate,
                t.IsCompleted,
                t.CompletedAtUtc))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<OnboardingTaskDto>>(items);
    }
}
