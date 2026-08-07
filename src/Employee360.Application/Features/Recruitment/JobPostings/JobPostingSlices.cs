using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Application.Common.Validation;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Recruitment.JobPostings;

public sealed record JobPostingDto(
    Guid Id,
    string Title,
    Guid? DepartmentId,
    string? DepartmentName,
    string Description,
    JobPostingStatus Status,
    DateOnly? ClosingDate,
    int CandidateCount);

// ---------------------------------------------------------------------------
// CRUD
// ---------------------------------------------------------------------------

public sealed record CreateJobPostingCommand(
    string Title,
    Guid? DepartmentId,
    string Description,
    DateOnly? ClosingDate) : IRequest<Result<Guid>>;

public sealed class CreateJobPostingValidator : AbstractValidator<CreateJobPostingCommand>
{
    public CreateJobPostingValidator()
    {
        RuleFor(c => c.Title).NotEmpty().MaximumLength(256);
        RuleFor(c => c.Description).NotEmpty();
    }
}

public sealed class CreateJobPostingHandler : IRequestHandler<CreateJobPostingCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateJobPostingHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(CreateJobPostingCommand request, CancellationToken cancellationToken)
    {
        if (request.DepartmentId.HasValue &&
            !await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value, cancellationToken))
        {
            return Result.Failure<Guid>("Department not found.");
        }

        var posting = new JobPosting
        {
            Title = request.Title.Trim(),
            DepartmentId = request.DepartmentId,
            Description = request.Description.Trim(),
            ClosingDate = request.ClosingDate,
            Status = JobPostingStatus.Draft,
        };

        _context.JobPostings.Add(posting);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success(posting.Id);
    }
}

public sealed record UpdateJobPostingCommand(
    Guid Id,
    string Title,
    Guid? DepartmentId,
    string Description,
    DateOnly? ClosingDate) : IRequest<Result>;

public sealed class UpdateJobPostingHandler : IRequestHandler<UpdateJobPostingCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateJobPostingHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateJobPostingCommand request, CancellationToken cancellationToken)
    {
        var posting = await _context.JobPostings.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (posting is null)
        {
            return Result.Failure("Job posting not found.");
        }

        if (posting.Status == JobPostingStatus.Closed)
        {
            return Result.Failure("Closed job postings cannot be edited.");
        }

        if (request.DepartmentId.HasValue &&
            !await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value, cancellationToken))
        {
            return Result.Failure("Department not found.");
        }

        posting.Title = request.Title.Trim();
        posting.DepartmentId = request.DepartmentId;
        posting.Description = request.Description.Trim();
        posting.ClosingDate = request.ClosingDate;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record PublishJobPostingCommand(Guid Id) : IRequest<Result>;

public sealed class PublishJobPostingHandler : IRequestHandler<PublishJobPostingCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _clock;

    public PublishJobPostingHandler(IApplicationDbContext context, IDateTimeProvider clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<Result> Handle(PublishJobPostingCommand request, CancellationToken cancellationToken)
    {
        var posting = await _context.JobPostings.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (posting is null)
        {
            return Result.Failure("Job posting not found.");
        }

        if (posting.Status == JobPostingStatus.Closed)
        {
            return Result.Failure("Closed job postings cannot be published.");
        }

        if (posting.ClosingDate.HasValue && posting.ClosingDate.Value < _clock.TodayWat)
        {
            return Result.Failure("Closing date must be today or in the future.");
        }

        posting.Status = JobPostingStatus.Published;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record CloseJobPostingCommand(Guid Id) : IRequest<Result>;

public sealed class CloseJobPostingHandler : IRequestHandler<CloseJobPostingCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public CloseJobPostingHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(CloseJobPostingCommand request, CancellationToken cancellationToken)
    {
        var posting = await _context.JobPostings.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (posting is null)
        {
            return Result.Failure("Job posting not found.");
        }

        posting.Status = JobPostingStatus.Closed;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record GetJobPostingsQuery(
    int Page = 1,
    int PageSize = 20,
    JobPostingStatus? Status = null,
    string? Search = null) : IRequest<Result<PagedResult<JobPostingDto>>>;

public sealed class GetJobPostingsValidator : AbstractValidator<GetJobPostingsQuery>
{
    public GetJobPostingsValidator()
    {
        RuleFor(q => q.Page).ValidPage();
        RuleFor(q => q.PageSize).ValidPageSize();
    }
}

public sealed class GetJobPostingsHandler : IRequestHandler<GetJobPostingsQuery, Result<PagedResult<JobPostingDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetJobPostingsHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<PagedResult<JobPostingDto>>> Handle(
        GetJobPostingsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.JobPostings.AsNoTracking();

        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(p => p.Title.Contains(term) || p.Description.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new JobPostingDto(
                p.Id,
                p.Title,
                p.DepartmentId,
                p.Department != null ? p.Department.Name : null,
                p.Description,
                p.Status,
                p.ClosingDate,
                p.Candidates.Count))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<JobPostingDto>(
            items, request.Page, request.PageSize, total));
    }
}

public sealed record GetJobPostingByIdQuery(Guid Id) : IRequest<Result<JobPostingDto>>;

public sealed class GetJobPostingByIdHandler : IRequestHandler<GetJobPostingByIdQuery, Result<JobPostingDto>>
{
    private readonly IApplicationDbContext _context;

    public GetJobPostingByIdHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<JobPostingDto>> Handle(
        GetJobPostingByIdQuery request,
        CancellationToken cancellationToken)
    {
        var item = await _context.JobPostings
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
            .Select(p => new JobPostingDto(
                p.Id,
                p.Title,
                p.DepartmentId,
                p.Department != null ? p.Department.Name : null,
                p.Description,
                p.Status,
                p.ClosingDate,
                p.Candidates.Count))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null
            ? Result.Failure<JobPostingDto>("Job posting not found.")
            : Result.Success(item);
    }
}

/// <summary>Public careers page listing (FR-REC-001 publish).</summary>
public sealed record GetPublishedJobsQuery : IRequest<Result<IReadOnlyList<JobPostingDto>>>;

public sealed class GetPublishedJobsHandler : IRequestHandler<GetPublishedJobsQuery, Result<IReadOnlyList<JobPostingDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _clock;

    public GetPublishedJobsHandler(IApplicationDbContext context, IDateTimeProvider clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<JobPostingDto>>> Handle(
        GetPublishedJobsQuery request,
        CancellationToken cancellationToken)
    {
        var today = _clock.TodayWat;

        var items = await _context.JobPostings
            .AsNoTracking()
            .Where(p =>
                p.Status == JobPostingStatus.Published &&
                (p.ClosingDate == null || p.ClosingDate >= today))
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new JobPostingDto(
                p.Id,
                p.Title,
                p.DepartmentId,
                p.Department != null ? p.Department.Name : null,
                p.Description,
                p.Status,
                p.ClosingDate,
                p.Candidates.Count))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<JobPostingDto>>(items);
    }
}
