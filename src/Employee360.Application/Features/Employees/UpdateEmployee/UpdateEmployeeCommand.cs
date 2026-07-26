using Employee360.Application.Features.Employees.CreateEmployee;
using Employee360.Domain.Common;
using Employee360.Domain.Enums;
using MediatR;

namespace Employee360.Application.Features.Employees.UpdateEmployee;

/// <summary>
/// Updates an employee's master data (FR-EMP-003; HR Admin operation).
/// Null optional fields clear the stored value; the employee code is immutable.
/// </summary>
public sealed record UpdateEmployeeCommand(
    Guid EmployeeId,
    string FirstName,
    string? MiddleName,
    string LastName,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    Gender Gender,
    MaritalStatus MaritalStatus,
    string? Nationality,
    string? Nin,
    string? Address,
    Guid? DepartmentId,
    Guid? PositionId,
    Guid? ManagerId,
    EmploymentType EmploymentType,
    DateOnly? JoinDate,
    string? WorkLocation,
    BankAccountInput? BankAccount) : IRequest<Result>;
