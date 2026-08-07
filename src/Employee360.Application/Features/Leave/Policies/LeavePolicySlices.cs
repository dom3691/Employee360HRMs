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

namespace Employee360.Application.Features.Leave.Policies;

public sealed record LeavePolicyDto(
    Guid Id,
    string Name,
    string Category,
    decimal DaysPerYear,
    decimal CarryOverDays,
    int NoticeDays,
    bool AllowHalfDay,
    bool RequireDocument,
    int? DocumentThresholdDays,
    bool ExcludeProbation,
    string Description,
    int SortOrder,
    bool IsActive);

public sealed record UpsertLeavePolicyRequest(
    string Name,
    string Category,
    decimal DaysPerYear,
    decimal CarryOverDays,
    int NoticeDays,
    bool AllowHalfDay,
    bool RequireDocument,
    int? DocumentThresholdDays,
    bool ExcludeProbation,
    string? Description);

public sealed record GetLeavePoliciesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null) : IRequest<Result<PagedResult<LeavePolicyDto>>>;

public sealed class GetLeavePoliciesValidator : AbstractValidator<GetLeavePoliciesQuery>
{
    public GetLeavePoliciesValidator()
    {
        RuleFor(q => q.Page).ValidPage();
        RuleFor(q => q.PageSize).ValidPageSize();
    }
}

public sealed class GetLeavePoliciesHandler : IRequestHandler<GetLeavePoliciesQuery, Result<PagedResult<LeavePolicyDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetLeavePoliciesHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<PagedResult<LeavePolicyDto>>> Handle(
        GetLeavePoliciesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.LeaveTypes
            .AsNoTracking()
            .Include(t => t.Policy)
            .Where(t => !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(t => t.Name.Contains(term) || (t.Category != null && t.Category.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var types = await query
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = types.Select(MapType).ToList();

        return Result.Success(new PagedResult<LeavePolicyDto>(
            items, request.Page, request.PageSize, totalCount));
    }

    internal static LeavePolicyDto MapType(LeaveType type)
    {
        var category = type.Category ?? InferCategory(type.Name, type.Code);
        var policy = type.Policy;

        return new LeavePolicyDto(
            type.Id,
            type.Name,
            category,
            policy?.AnnualEntitlement ?? 0m,
            policy?.CarryForwardMax ?? 0m,
            type.NoticeDays,
            type.AllowHalfDay,
            type.RequiresAttachment,
            type.DocumentThresholdDays,
            (policy?.ProbationMonths ?? 0) > 0,
            type.PolicyDescription ?? string.Empty,
            type.SortOrder,
            type.IsActive);
    }

    private static string InferCategory(string name, string code)
    {
        var value = (name + " " + code).ToLowerInvariant();
        if (value.Contains("annual")) return "Annual";
        if (value.Contains("sick")) return "Sick";
        if (value.Contains("maternity")) return "Maternity";
        if (value.Contains("paternity")) return "Paternity";
        if (value.Contains("casual")) return "Casual";
        if (value.Contains("study")) return "Study";
        if (value.Contains("unpaid")) return "Unpaid";
        return "Annual";
    }
}

public sealed record CreateLeavePolicyCommand(UpsertLeavePolicyRequest Request) : IRequest<Result<Guid>>;

public sealed class CreateLeavePolicyValidator : AbstractValidator<CreateLeavePolicyCommand>
{
    public CreateLeavePolicyValidator()
    {
        RuleFor(c => c.Request.Name).NotEmpty().MaximumLength(64);
        RuleFor(c => c.Request.Category).NotEmpty().MaximumLength(32);
        RuleFor(c => c.Request.DaysPerYear).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateLeavePolicyHandler : IRequestHandler<CreateLeavePolicyCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateLeavePolicyHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(CreateLeavePolicyCommand request, CancellationToken cancellationToken)
    {
        var body = request.Request;
        var code = GenerateCode(body.Name);

        if (await _context.LeaveTypes.AnyAsync(t => t.Code == code && !t.IsDeleted, cancellationToken))
        {
            code = $"{code}{Random.Shared.Next(10, 99)}";
        }

        var type = new LeaveType
        {
            Name = body.Name.Trim(),
            Code = code,
            Category = body.Category.Trim(),
            PolicyDescription = body.Description?.Trim(),
            NoticeDays = body.NoticeDays,
            AllowHalfDay = body.AllowHalfDay,
            RequiresAttachment = body.RequireDocument,
            DocumentThresholdDays = body.DocumentThresholdDays,
            IsActive = true,
            SortOrder = await _context.LeaveTypes.CountAsync(cancellationToken),
            Policy = new Domain.Entities.LeavePolicy
            {
                AnnualEntitlement = body.DaysPerYear,
                CarryForwardMax = body.CarryOverDays,
                ProbationMonths = body.ExcludeProbation ? 3 : 0,
                AccrualFrequency = AccrualFrequency.Annual,
            },
        };

        _context.LeaveTypes.Add(type);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success(type.Id);
    }

    private static string GenerateCode(string name)
    {
        var letters = new string(name.Where(char.IsLetterOrDigit).Take(3).ToArray()).ToUpperInvariant();
        return string.IsNullOrEmpty(letters) ? "LV" : letters;
    }
}

public sealed record UpdateLeavePolicyCommand(Guid Id, UpsertLeavePolicyRequest Request) : IRequest<Result>;

public sealed class UpdateLeavePolicyHandler : IRequestHandler<UpdateLeavePolicyCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateLeavePolicyHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateLeavePolicyCommand request, CancellationToken cancellationToken)
    {
        var type = await _context.LeaveTypes
            .Include(t => t.Policy)
            .FirstOrDefaultAsync(t => t.Id == request.Id && !t.IsDeleted, cancellationToken);

        if (type is null)
        {
            return Result.Failure("Leave policy not found.");
        }

        var body = request.Request;
        type.Name = body.Name.Trim();
        type.Category = body.Category.Trim();
        type.PolicyDescription = body.Description?.Trim();
        type.NoticeDays = body.NoticeDays;
        type.AllowHalfDay = body.AllowHalfDay;
        type.RequiresAttachment = body.RequireDocument;
        type.DocumentThresholdDays = body.DocumentThresholdDays;

        if (type.Policy is null)
        {
            type.Policy = new Domain.Entities.LeavePolicy { LeaveTypeId = type.Id };
            _context.LeavePolicies.Add(type.Policy);
        }

        type.Policy.AnnualEntitlement = body.DaysPerYear;
        type.Policy.CarryForwardMax = body.CarryOverDays;
        type.Policy.ProbationMonths = body.ExcludeProbation ? Math.Max(type.Policy.ProbationMonths, 1) : 0;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record DeleteLeavePolicyCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteLeavePolicyHandler : IRequestHandler<DeleteLeavePolicyCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _clock;

    public DeleteLeavePolicyHandler(IApplicationDbContext context, IDateTimeProvider clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<Result> Handle(DeleteLeavePolicyCommand request, CancellationToken cancellationToken)
    {
        var type = await _context.LeaveTypes
            .FirstOrDefaultAsync(t => t.Id == request.Id && !t.IsDeleted, cancellationToken);

        if (type is null)
        {
            return Result.Failure("Leave policy not found.");
        }

        type.IsDeleted = true;
        type.DeletedAt = _clock.UtcNow;
        type.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
