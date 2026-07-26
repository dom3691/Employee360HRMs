using Employee360.Domain.Common;
using Employee360.Domain.Enums;
using MediatR;

namespace Employee360.Application.Features.Employees.CreateEmployee;

/// <summary>Bank details supplied at employee creation (FR-EMP-014).</summary>
/// <param name="BankName">Bank name.</param>
/// <param name="AccountNumber">Plain account number — encrypted before persistence.</param>
/// <param name="AccountName">Account holder name.</param>
public sealed record BankAccountInput(string BankName, string AccountNumber, string AccountName);

/// <summary>Emergency contact / next of kin supplied at creation (FR-EMP-013).</summary>
/// <param name="Type">Contact type.</param>
/// <param name="FullName">Contact name.</param>
/// <param name="Relationship">Relationship to the employee.</param>
/// <param name="PhoneNumber">Contact phone.</param>
/// <param name="Address">Contact address (optional).</param>
public sealed record ContactInput(
    ContactType Type,
    string FullName,
    string Relationship,
    string PhoneNumber,
    string? Address);

/// <summary>
/// Creates an employee master record with an auto-generated employee code
/// (FR-EMP-001..003). Created records are immediately Active per PRD 7.2
/// acceptance criteria.
/// </summary>
public sealed record CreateEmployeeCommand(
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
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
    BankAccountInput? BankAccount,
    IReadOnlyCollection<ContactInput>? Contacts) : IRequest<Result<CreateEmployeeResponse>>;
