namespace Employee360.Application.Features.Employees.CreateEmployee;

/// <summary>Created employee identifiers.</summary>
/// <param name="Id">The employee's Guid primary key.</param>
/// <param name="EmployeeCode">The generated human-readable code, e.g. "EMP-00001".</param>
public sealed record CreateEmployeeResponse(Guid Id, string EmployeeCode);
