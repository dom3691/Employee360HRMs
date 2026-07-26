using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Employees.SubmitProfileChangeRequest;

/// <summary>
/// Employee self-service: requests changes to whitelisted profile fields
/// (PhoneNumber, Address) pending HR approval (FR-EMP-011). The requesting
/// employee is resolved from the authenticated user.
/// </summary>
/// <param name="Changes">Field name → requested new value.</param>
public sealed record SubmitProfileChangeRequestCommand(
    IReadOnlyDictionary<string, string> Changes) : IRequest<Result<Guid>>;
