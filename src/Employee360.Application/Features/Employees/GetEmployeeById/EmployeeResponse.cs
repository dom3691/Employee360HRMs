using Employee360.Domain.Enums;

namespace Employee360.Application.Features.Employees.GetEmployeeById;

/// <summary>Masked bank details (account number shows last 4 digits only).</summary>
public sealed record BankAccountResponse(string BankName, string AccountNumberMasked, string AccountName);

/// <summary>Contact person payload.</summary>
public sealed record ContactResponse(
    Guid Id,
    ContactType Type,
    string FullName,
    string Relationship,
    string PhoneNumber,
    string? Address);

/// <summary>
/// Full employee profile. Sensitive fields (NIN, account number) are always
/// masked (NFR-SEC-004); full values never leave the API.
/// </summary>
public sealed record EmployeeResponse(
    Guid Id,
    string EmployeeCode,
    string FirstName,
    string? MiddleName,
    string LastName,
    string FullName,
    string Email,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    Gender Gender,
    MaritalStatus MaritalStatus,
    string? Nationality,
    string? NinMasked,
    string? Address,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? PositionId,
    string? PositionTitle,
    Guid? ManagerId,
    string? ManagerName,
    EmploymentType EmploymentType,
    DateOnly? JoinDate,
    string? WorkLocation,
    EmployeeStatus Status,
    BankAccountResponse? BankAccount,
    IReadOnlyList<ContactResponse> Contacts);
