using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Services;
using Employee360.Application.Features.Employees.CreateEmployee;
using Employee360.Domain.Common;
using Employee360.Domain.Constants;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Recruitment.Candidates;

public sealed record CandidateDto(
    Guid Id,
    Guid JobPostingId,
    string JobTitle,
    string Name,
    string Email,
    string? Phone,
    CandidateStage Stage,
    string? ResumePath,
    Guid? EmployeeId);

// ---------------------------------------------------------------------------
// Apply / CRUD
// ---------------------------------------------------------------------------

public sealed record ApplyForJobCommand(
    Guid JobPostingId,
    string Name,
    string Email,
    string? Phone) : IRequest<Result<Guid>>;

public sealed class ApplyForJobValidator : AbstractValidator<ApplyForJobCommand>
{
    public ApplyForJobValidator()
    {
        RuleFor(c => c.JobPostingId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(256);
        RuleFor(c => c.Email).NotEmpty().EmailAddress();
    }
}

public sealed class ApplyForJobHandler : IRequestHandler<ApplyForJobCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _clock;
    private readonly IRecruitmentNotifier _notifier;

    public ApplyForJobHandler(
        IApplicationDbContext context,
        IDateTimeProvider clock,
        IRecruitmentNotifier notifier)
    {
        _context = context;
        _clock = clock;
        _notifier = notifier;
    }

    public async Task<Result<Guid>> Handle(ApplyForJobCommand request, CancellationToken cancellationToken)
    {
        var posting = await _context.JobPostings
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.JobPostingId, cancellationToken);

        if (posting is null)
        {
            return Result.Failure<Guid>("Job posting not found.");
        }

        if (posting.Status != JobPostingStatus.Published)
        {
            return Result.Failure<Guid>("This job is not accepting applications.");
        }

        if (posting.ClosingDate.HasValue && posting.ClosingDate.Value < _clock.TodayWat)
        {
            return Result.Failure<Guid>("The application deadline has passed.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var duplicate = await _context.Candidates.AnyAsync(
            c => c.JobPostingId == request.JobPostingId && c.Email.ToLower() == email,
            cancellationToken);

        if (duplicate)
        {
            return Result.Failure<Guid>("You have already applied for this job.");
        }

        var candidate = new Candidate
        {
            JobPostingId = request.JobPostingId,
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone?.Trim(),
            Stage = CandidateStage.Applied,
        };

        _context.Candidates.Add(candidate);
        await _context.SaveChangesAsync(cancellationToken);

        await _notifier.NotifyTeamAsync(
            $"New application: {candidate.Name}",
            $"Candidate {candidate.Name} applied for {posting.Title}.",
            cancellationToken);

        return Result.Success(candidate.Id);
    }
}

public sealed record GetCandidatesQuery(
    Guid? JobPostingId,
    CandidateStage? Stage) : IRequest<Result<IReadOnlyList<CandidateDto>>>;

public sealed class GetCandidatesHandler : IRequestHandler<GetCandidatesQuery, Result<IReadOnlyList<CandidateDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetCandidatesHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<IReadOnlyList<CandidateDto>>> Handle(
        GetCandidatesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Candidates.AsNoTracking();

        if (request.JobPostingId.HasValue)
        {
            query = query.Where(c => c.JobPostingId == request.JobPostingId.Value);
        }

        if (request.Stage.HasValue)
        {
            query = query.Where(c => c.Stage == request.Stage.Value);
        }

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CandidateDto(
                c.Id,
                c.JobPostingId,
                c.JobPosting!.Title,
                c.Name,
                c.Email,
                c.Phone,
                c.Stage,
                c.ResumePath,
                c.EmployeeId))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CandidateDto>>(items);
    }
}

public sealed record GetCandidateByIdQuery(Guid Id) : IRequest<Result<CandidateDto>>;

public sealed class GetCandidateByIdHandler : IRequestHandler<GetCandidateByIdQuery, Result<CandidateDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCandidateByIdHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<CandidateDto>> Handle(
        GetCandidateByIdQuery request,
        CancellationToken cancellationToken)
    {
        var item = await _context.Candidates
            .AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(c => new CandidateDto(
                c.Id,
                c.JobPostingId,
                c.JobPosting!.Title,
                c.Name,
                c.Email,
                c.Phone,
                c.Stage,
                c.ResumePath,
                c.EmployeeId))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null
            ? Result.Failure<CandidateDto>("Candidate not found.")
            : Result.Success(item);
    }
}

// ---------------------------------------------------------------------------
// Stage transition
// ---------------------------------------------------------------------------

public sealed record TransitionCandidateStageCommand(Guid CandidateId, CandidateStage Stage)
    : IRequest<Result>;

public sealed class TransitionCandidateStageHandler : IRequestHandler<TransitionCandidateStageCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IRecruitmentNotifier _notifier;

    public TransitionCandidateStageHandler(
        IApplicationDbContext context,
        IRecruitmentNotifier notifier)
    {
        _context = context;
        _notifier = notifier;
    }

    public async Task<Result> Handle(
        TransitionCandidateStageCommand request,
        CancellationToken cancellationToken)
    {
        var candidate = await _context.Candidates
            .Include(c => c.JobPosting)
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId, cancellationToken);

        if (candidate is null)
        {
            return Result.Failure("Candidate not found.");
        }

        var transition = CandidatePipelineWorkflow.EnsureTransition(candidate.Stage, request.Stage);
        if (transition.IsFailure)
        {
            return transition;
        }

        candidate.Stage = request.Stage;
        await _context.SaveChangesAsync(cancellationToken);

        await _notifier.NotifyTeamAsync(
            $"Candidate moved to {request.Stage}",
            $"{candidate.Name} is now at stage {request.Stage} for {candidate.JobPosting.Title}.",
            cancellationToken);

        return Result.Success();
    }
}

// ---------------------------------------------------------------------------
// Resume upload
// ---------------------------------------------------------------------------

public sealed record UploadCandidateResumeCommand(
    Guid CandidateId,
    string FileName,
    string ContentType,
    byte[] Content) : IRequest<Result<string>>;

public sealed class UploadCandidateResumeValidator : AbstractValidator<UploadCandidateResumeCommand>
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    };

    public UploadCandidateResumeValidator()
    {
        RuleFor(c => c.CandidateId).NotEmpty();
        RuleFor(c => c.FileName).NotEmpty();
        RuleFor(c => c.Content).NotEmpty();
        RuleFor(c => c.ContentType)
            .Must(t => AllowedTypes.Contains(t))
            .WithMessage("Resume must be PDF or Word document.");
    }
}

public sealed class UploadCandidateResumeHandler : IRequestHandler<UploadCandidateResumeCommand, Result<string>>
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;

    public UploadCandidateResumeHandler(IApplicationDbContext context, IFileStorageService fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }

    public async Task<Result<string>> Handle(
        UploadCandidateResumeCommand request,
        CancellationToken cancellationToken)
    {
        var candidate = await _context.Candidates.FirstOrDefaultAsync(
            c => c.Id == request.CandidateId, cancellationToken);

        if (candidate is null)
        {
            return Result.Failure<string>("Candidate not found.");
        }

        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        var path = $"resumes/{request.CandidateId}/{Guid.NewGuid():N}{extension}";

        await _fileStorage.UploadAsync(path, request.Content, request.ContentType, cancellationToken);
        candidate.ResumePath = path;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(path);
    }
}

// ---------------------------------------------------------------------------
// Offer letter (Should — configurable template)
// ---------------------------------------------------------------------------

public sealed record GenerateOfferLetterQuery(Guid CandidateId) : IRequest<Result<OfferLetterDto>>;

public sealed record OfferLetterDto(string Subject, string BodyHtml);

public sealed class GenerateOfferLetterHandler : IRequestHandler<GenerateOfferLetterQuery, Result<OfferLetterDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailTemplateService _templates;
    private readonly IDateTimeProvider _clock;

    public GenerateOfferLetterHandler(
        IApplicationDbContext context,
        IEmailTemplateService templates,
        IDateTimeProvider clock)
    {
        _context = context;
        _templates = templates;
        _clock = clock;
    }

    public async Task<Result<OfferLetterDto>> Handle(
        GenerateOfferLetterQuery request,
        CancellationToken cancellationToken)
    {
        var candidate = await _context.Candidates
            .AsNoTracking()
            .Include(c => c.JobPosting)
            .ThenInclude(j => j!.Department)
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId, cancellationToken);

        if (candidate is null)
        {
            return Result.Failure<OfferLetterDto>("Candidate not found.");
        }

        if (candidate.Stage is not (CandidateStage.Offer or CandidateStage.Hired))
        {
            return Result.Failure<OfferLetterDto>("Offer letter is available at Offer stage or later.");
        }

        var tokens = new Dictionary<string, string>
        {
            ["CandidateName"] = candidate.Name,
            ["JobTitle"] = candidate.JobPosting.Title,
            ["Department"] = candidate.JobPosting.Department?.Name ?? "the organization",
            ["OfferDate"] = _clock.TodayWat.ToString("dd MMMM yyyy"),
            ["StartDate"] = _clock.TodayWat.AddDays(14).ToString("dd MMMM yyyy"),
        };

        var rendered = await _templates.RenderAsync(
            EmailTemplateCodes.OfferLetter, tokens, cancellationToken);

        return Result.Success(new OfferLetterDto(rendered.Subject, rendered.BodyHtml));
    }
}

// ---------------------------------------------------------------------------
// Convert to employee (FR-REC-005)
// ---------------------------------------------------------------------------

public sealed record ConvertCandidateToEmployeeCommand(
    Guid CandidateId,
    Guid? PositionId,
    Guid? ManagerId,
    DateOnly? JoinDate) : IRequest<Result<CreateEmployeeResponse>>;

public sealed class ConvertCandidateToEmployeeHandler
    : IRequestHandler<ConvertCandidateToEmployeeCommand, Result<CreateEmployeeResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMediator _mediator;

    public ConvertCandidateToEmployeeHandler(IApplicationDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<Result<CreateEmployeeResponse>> Handle(
        ConvertCandidateToEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        var candidate = await _context.Candidates
            .Include(c => c.JobPosting)
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId, cancellationToken);

        if (candidate is null)
        {
            return Result.Failure<CreateEmployeeResponse>("Candidate not found.");
        }

        if (candidate.EmployeeId.HasValue)
        {
            return Result.Failure<CreateEmployeeResponse>("Candidate has already been converted to an employee.");
        }

        if (candidate.Stage != CandidateStage.Hired)
        {
            return Result.Failure<CreateEmployeeResponse>(
                "Only candidates at Hired stage can be converted to employees.");
        }

        var (firstName, lastName) = CandidatePipelineWorkflow.SplitName(candidate.Name);

        var createResult = await _mediator.Send(
            new CreateEmployeeCommand(
                FirstName: firstName,
                MiddleName: null,
                LastName: lastName,
                Email: candidate.Email,
                PhoneNumber: candidate.Phone,
                DateOfBirth: null,
                Gender: Gender.PreferNotToSay,
                MaritalStatus: MaritalStatus.Single,
                Nationality: "Nigerian",
                Nin: null,
                Address: null,
                DepartmentId: candidate.JobPosting.DepartmentId,
                PositionId: request.PositionId,
                ManagerId: request.ManagerId,
                EmploymentType: EmploymentType.FullTime,
                JoinDate: request.JoinDate,
                WorkLocation: null,
                BankAccount: null,
                Contacts: null),
            cancellationToken);

        if (createResult.IsFailure)
        {
            return createResult;
        }

        candidate.EmployeeId = createResult.Value!.Id;
        await _context.SaveChangesAsync(cancellationToken);

        return createResult;
    }
}
