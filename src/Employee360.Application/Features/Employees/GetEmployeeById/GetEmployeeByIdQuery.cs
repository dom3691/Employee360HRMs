using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Employees.GetEmployeeById;

/// <summary>Fetches a single employee's full profile with masked PII.</summary>
/// <param name="EmployeeId">The employee id.</param>
public sealed record GetEmployeeByIdQuery(Guid EmployeeId) : IRequest<Result<EmployeeResponse>>;
