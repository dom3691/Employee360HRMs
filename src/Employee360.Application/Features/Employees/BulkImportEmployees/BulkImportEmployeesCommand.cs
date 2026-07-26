using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Employees.BulkImportEmployees;

/// <summary>A row that failed to import, with its 1-based CSV row number.</summary>
public sealed record ImportRowError(int Row, string Error);

/// <summary>Bulk import outcome.</summary>
public sealed record BulkImportEmployeesResponse(
    int ImportedCount,
    IReadOnlyList<ImportRowError> Errors);

/// <summary>
/// Bulk-imports employees from CSV (FR-EMP-012, Could). Expected header:
/// FirstName,LastName,Email,PhoneNumber,JoinDate — JoinDate as yyyy-MM-dd.
/// Valid rows are imported; invalid rows are reported per line.
/// </summary>
/// <param name="CsvContent">UTF-8 CSV file content.</param>
public sealed record BulkImportEmployeesCommand(byte[] CsvContent)
    : IRequest<Result<BulkImportEmployeesResponse>>;
