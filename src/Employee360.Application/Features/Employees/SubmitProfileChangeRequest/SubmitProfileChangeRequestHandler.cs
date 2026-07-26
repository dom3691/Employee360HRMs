using System.Text.Json;
using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Employees.SubmitProfileChangeRequest;

/// <summary>
/// Handles <see cref="SubmitProfileChangeRequestCommand"/> for the authenticated
/// employee; one pending request at a time keeps the review queue unambiguous.
/// </summary>
public sealed class SubmitProfileChangeRequestHandler
    : IRequestHandler<SubmitProfileChangeRequestCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SubmitProfileChangeRequestHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(
        SubmitProfileChangeRequestCommand request,
        CancellationToken cancellationToken)
    {
        var employeeId = _currentUserService.EmployeeId;

        if (employeeId is null)
        {
            return Result.Failure<Guid>("No employee record is linked to your account.");
        }

        var employeeExists = await _context.Employees
            .AnyAsync(e => e.Id == employeeId.Value, cancellationToken);

        if (!employeeExists)
        {
            return Result.Failure<Guid>("Employee record not found.");
        }

        var hasPending = await _context.ProfileChangeRequests
            .AnyAsync(
                r => r.EmployeeId == employeeId.Value &&
                     r.Status == ProfileChangeRequestStatus.Pending,
                cancellationToken);

        if (hasPending)
        {
            return Result.Failure<Guid>(
                "You already have a pending profile change request. Wait for HR to review it.");
        }

        var changeRequest = new ProfileChangeRequest
        {
            EmployeeId = employeeId.Value,
            ChangesJson = JsonSerializer.Serialize(request.Changes),
            Status = ProfileChangeRequestStatus.Pending,
        };

        _context.ProfileChangeRequests.Add(changeRequest);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(changeRequest.Id);
    }
}
