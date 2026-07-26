using System.Text.Json;
using Employee360.Application.Common.Interfaces;
using Employee360.Application.Features.Employees.SubmitProfileChangeRequest;
using Employee360.Domain.Common;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Employees.ReviewProfileChangeRequest;

/// <summary>
/// Handles <see cref="ReviewProfileChangeRequestCommand"/>: on approval the
/// whitelisted field changes are applied to the employee record (audited by the
/// interceptor); the decision is stamped with reviewer and time either way.
/// </summary>
public sealed class ReviewProfileChangeRequestHandler
    : IRequestHandler<ReviewProfileChangeRequestCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReviewProfileChangeRequestHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(
        ReviewProfileChangeRequestCommand request,
        CancellationToken cancellationToken)
    {
        var changeRequest = await _context.ProfileChangeRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken);

        if (changeRequest is null)
        {
            return Result.Failure("Profile change request not found.");
        }

        if (changeRequest.Status != ProfileChangeRequestStatus.Pending)
        {
            return Result.Failure("This request has already been reviewed.");
        }

        if (request.Approve)
        {
            var changes = JsonSerializer
                .Deserialize<Dictionary<string, string>>(changeRequest.ChangesJson) ?? [];

            foreach (var (field, value) in changes)
            {
                // Only whitelisted fields can have been submitted, but re-check
                // defensively in case the whitelist shrank since submission.
                if (!SubmitProfileChangeRequestValidator.AllowedFields.Contains(field))
                {
                    continue;
                }

                switch (field)
                {
                    case "PhoneNumber":
                        changeRequest.Employee.PhoneNumber = value;
                        break;
                    case "Address":
                        changeRequest.Employee.Address = value;
                        break;
                }
            }

            changeRequest.Status = ProfileChangeRequestStatus.Approved;
        }
        else
        {
            changeRequest.Status = ProfileChangeRequestStatus.Rejected;
        }

        changeRequest.ReviewedBy = _currentUserService.UserId;
        changeRequest.ReviewedAtUtc = _dateTimeProvider.UtcNow;
        changeRequest.ReviewComment = request.Comment;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
