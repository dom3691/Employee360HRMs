using System.Globalization;
using System.Text;
using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Application.Common.Validation;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.AuditLogs;

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/// <summary>Audit log list item aligned with frontend contract (FR-ADM-004).</summary>
public sealed record AuditLogListItem(
    Guid Id,
    string Action,
    string Module,
    string Description,
    string? EntityName,
    string? EntityId,
    string? Resource,
    string? ResourceId,
    Guid? UserId,
    string? UserName,
    string? UserRole,
    string? IpAddress,
    string? UserAgent,
    string Outcome,
    DateTime TimestampUtc,
    string? Details);

// ---------------------------------------------------------------------------
// GetAuditLogsPaged
// ---------------------------------------------------------------------------

/// <summary>
/// Queries audit logs with optional filters (FR-ADM-004).
/// </summary>
public sealed record GetAuditLogsPagedQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? UserId = null,
    string? EntityName = null,
    string? Action = null,
    string? Module = null,
    string? Outcome = null,
    string? Search = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IRequest<Result<PagedResult<AuditLogListItem>>>;

/// <summary>Input validation for <see cref="GetAuditLogsPagedQuery"/>.</summary>
public sealed class GetAuditLogsPagedValidator : AbstractValidator<GetAuditLogsPagedQuery>
{
    public GetAuditLogsPagedValidator()
    {
        RuleFor(q => q.Page).ValidPage();
        RuleFor(q => q.PageSize).ValidPageSize();

        RuleFor(q => q)
            .Must(q => q.FromDate is null || q.ToDate is null || q.FromDate <= q.ToDate)
            .WithMessage("FromDate must be on or before ToDate.");
    }
}

/// <summary>Handles <see cref="GetAuditLogsPagedQuery"/>.</summary>
public sealed class GetAuditLogsPagedHandler
    : IRequestHandler<GetAuditLogsPagedQuery, Result<PagedResult<AuditLogListItem>>>
{
    private readonly IApplicationDbContext _context;

    public GetAuditLogsPagedHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<AuditLogListItem>>> Handle(
        GetAuditLogsPagedQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.AuditLogs.AsNoTracking();

        if (request.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == request.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityName))
        {
            var entityName = request.EntityName.Trim();
            query = query.Where(a => a.EntityName == entityName);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            var action = request.Action.Trim();
            query = query.Where(a => a.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(a =>
                a.EntityName.Contains(term) ||
                a.Action.Contains(term) ||
                (a.OldValues != null && a.OldValues.Contains(term)) ||
                (a.NewValues != null && a.NewValues.Contains(term)));
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= request.ToDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .GroupJoin(
                _context.Users.AsNoTracking(),
                a => a.UserId,
                u => u.Id,
                (a, users) => new { Log = a, User = users.FirstOrDefault() })
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => MapItem(r.Log, r.User)).ToList();

        if (!string.IsNullOrWhiteSpace(request.Module))
        {
            var module = request.Module.Trim();
            items = items.Where(i => i.Module.Equals(module, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(request.Outcome))
        {
            var outcome = request.Outcome.Trim();
            items = items.Where(i => i.Outcome.Equals(outcome, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return Result.Success(new PagedResult<AuditLogListItem>(
            items, request.Page, request.PageSize, totalCount));
    }

    internal static AuditLogListItem MapItem(AuditLog log, User? user)
    {
        var module = InferModule(log.EntityName);
        var outcome = log.Action.Contains("Fail", StringComparison.OrdinalIgnoreCase) ? "Failure" : "Success";

        return new AuditLogListItem(
            log.Id,
            NormalizeAction(log.Action),
            module,
            BuildDescription(log),
            log.EntityName,
            log.EntityId,
            log.EntityName,
            log.EntityId,
            log.UserId,
            user != null ? user.Email : null,
            null,
            null,
            null,
            outcome,
            log.Timestamp,
            log.NewValues ?? log.OldValues);
    }

    private static string InferModule(string entityName) => entityName switch
    {
        "LeaveRequest" or "LeaveType" or "LeavePolicy" or "PublicHoliday" => "Leave",
        "Employee" or "EmployeeDocument" or "ProfileChangeRequest" => "Employee",
        "PayrollRun" or "Payslip" or "SalaryStructure" => "Payroll",
        "JobPosting" or "Candidate" => "Recruitment",
        "PerformanceReview" or "ReviewCycle" or "EmployeeGoal" => "Performance",
        "Department" or "Position" or "Grade" or "Company" => "Organization",
        "Role" or "UserRole" or "EmailTemplate" => "Roles",
        "User" or "RefreshToken" => "Auth",
        "AuditLog" => "Audit",
        _ => "System",
    };

    private static string NormalizeAction(string action) => action switch
    {
        "Created" => "Create",
        "Updated" => "Update",
        "Deleted" => "Delete",
        "PermissionsUpdated" => "Update",
        _ => action,
    };

    private static string BuildDescription(AuditLog log)
        => $"{NormalizeAction(log.Action)} {log.EntityName} ({log.EntityId})";
}

// ---------------------------------------------------------------------------
// ExportAuditLogsCsv
// ---------------------------------------------------------------------------

/// <summary>Exports filtered audit logs as CSV (FR-ADM-004).</summary>
public sealed record ExportAuditLogsCsvQuery(
    Guid? UserId = null,
    string? EntityName = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IRequest<Result<byte[]>>;

/// <summary>Input validation for <see cref="ExportAuditLogsCsvQuery"/>.</summary>
public sealed class ExportAuditLogsCsvValidator : AbstractValidator<ExportAuditLogsCsvQuery>
{
    public ExportAuditLogsCsvValidator()
    {
        RuleFor(q => q)
            .Must(q => q.FromDate is null || q.ToDate is null || q.FromDate <= q.ToDate)
            .WithMessage("FromDate must be on or before ToDate.");
    }
}

/// <summary>Handles <see cref="ExportAuditLogsCsvQuery"/>.</summary>
public sealed class ExportAuditLogsCsvHandler : IRequestHandler<ExportAuditLogsCsvQuery, Result<byte[]>>
{
    private readonly IApplicationDbContext _context;

    public ExportAuditLogsCsvHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<byte[]>> Handle(
        ExportAuditLogsCsvQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.AuditLogs.AsNoTracking();

        if (request.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == request.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityName))
        {
            var entityName = request.EntityName.Trim();
            query = query.Where(a => a.EntityName == entityName);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= request.ToDate.Value);
        }

        var rows = await query
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);

        var csv = BuildCsv(rows);
        return Result.Success(Encoding.UTF8.GetBytes(csv));
    }

    public static string BuildCsv(IReadOnlyList<AuditLog> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,EntityName,EntityId,Action,UserId,Timestamp,OldValues,NewValues");

        foreach (var row in rows)
        {
            sb.Append(Escape(row.Id.ToString()));
            sb.Append(',');
            sb.Append(Escape(row.EntityName));
            sb.Append(',');
            sb.Append(Escape(row.EntityId));
            sb.Append(',');
            sb.Append(Escape(row.Action));
            sb.Append(',');
            sb.Append(Escape(row.UserId?.ToString() ?? string.Empty));
            sb.Append(',');
            sb.Append(Escape(row.Timestamp.ToString("O", CultureInfo.InvariantCulture)));
            sb.Append(',');
            sb.Append(Escape(row.OldValues ?? string.Empty));
            sb.Append(',');
            sb.Append(Escape(row.NewValues ?? string.Empty));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string Escape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }
}
