using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using Employee360.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Employees.DeactivateEmployee;

/// <summary>
/// Handles <see cref="DeactivateEmployeeCommand"/>: applies the lifecycle status,
/// disables the linked user account (deactivated employees cannot log in — PRD M1
/// acceptance criteria), revokes refresh tokens, and records the reason in an
/// explicit audit entry.
/// </summary>
public sealed class DeactivateEmployeeHandler : IRequestHandler<DeactivateEmployeeCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeactivateEmployeeHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeactivateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee is null)
        {
            return Result.Failure("Employee not found.");
        }

        if (employee.Status == request.NewStatus)
        {
            return Result.Failure($"Employee is already {request.NewStatus}.");
        }

        var utcNow = _dateTimeProvider.UtcNow;
        var previousStatus = employee.Status;

        employee.Status = request.NewStatus;

        // Disable login for the linked account and kill active sessions.
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.EmployeeId == employee.Id, cancellationToken);

        if (user is not null)
        {
            user.IsActive = false;

            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && rt.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
            {
                token.RevokedAtUtc = utcNow;
            }
        }

        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = nameof(Employee),
            EntityId = employee.Id.ToString(),
            Action = "Deactivated",
            OldValues = $$"""{"status":"{{previousStatus}}"}""",
            NewValues = $$"""{"status":"{{request.NewStatus}}","reason":"{{request.Reason.Replace("\"", "'")}}"}""",
            UserId = _currentUserService.UserId,
            Timestamp = utcNow,
        });

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
