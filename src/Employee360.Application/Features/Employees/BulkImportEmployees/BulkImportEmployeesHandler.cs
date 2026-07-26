using System.Globalization;
using System.Text;
using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Employee360.Application.Features.Employees.BulkImportEmployees;

/// <summary>
/// Handles <see cref="BulkImportEmployeesCommand"/>: parses the CSV, validates
/// each row (required fields, email format and uniqueness), and imports valid
/// rows with generated employee codes. Row failures never abort the batch.
/// </summary>
public sealed class BulkImportEmployeesHandler
    : IRequestHandler<BulkImportEmployeesCommand, Result<BulkImportEmployeesResponse>>
{
    private static readonly string[] ExpectedHeader =
        ["FirstName", "LastName", "Email", "PhoneNumber", "JoinDate"];

    private readonly IApplicationDbContext _context;
    private readonly EmployeeSettings _settings;

    public BulkImportEmployeesHandler(
        IApplicationDbContext context,
        IOptions<EmployeeSettings> settings)
    {
        _context = context;
        _settings = settings.Value;
    }

    /// <inheritdoc />
    public async Task<Result<BulkImportEmployeesResponse>> Handle(
        BulkImportEmployeesCommand request,
        CancellationToken cancellationToken)
    {
        var lines = Encoding.UTF8.GetString(request.CsvContent)
            .Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .ToList();

        if (lines.Count == 0 || string.IsNullOrWhiteSpace(lines[0]))
        {
            return Result.Failure<BulkImportEmployeesResponse>("CSV file is empty.");
        }

        var header = lines[0].Split(',').Select(h => h.Trim()).ToArray();

        if (!header.SequenceEqual(ExpectedHeader, StringComparer.OrdinalIgnoreCase))
        {
            return Result.Failure<BulkImportEmployeesResponse>(
                $"CSV header must be exactly: {string.Join(",", ExpectedHeader)}.");
        }

        var existingEmails = (await _context.Employees
                .IgnoreQueryFilters()
                .Select(e => e.Email.ToLower())
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var nextSequence = await GetNextSequenceAsync(cancellationToken);

        var errors = new List<ImportRowError>();
        var imported = 0;

        for (var i = 1; i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            var rowNumber = i + 1; // 1-based, including header
            var cells = lines[i].Split(',').Select(c => c.Trim()).ToArray();

            if (cells.Length < 3)
            {
                errors.Add(new ImportRowError(rowNumber, "Row must have at least FirstName, LastName, and Email."));
                continue;
            }

            var firstName = cells[0];
            var lastName = cells[1];
            var email = cells[2];
            var phone = cells.Length > 3 ? cells[3] : null;
            var joinDateRaw = cells.Length > 4 ? cells[4] : null;

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                errors.Add(new ImportRowError(rowNumber, "FirstName and LastName are required."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                errors.Add(new ImportRowError(rowNumber, $"'{email}' is not a valid email."));
                continue;
            }

            if (!existingEmails.Add(email.ToLower()))
            {
                errors.Add(new ImportRowError(rowNumber, $"Email '{email}' already exists."));
                continue;
            }

            DateOnly? joinDate = null;
            if (!string.IsNullOrWhiteSpace(joinDateRaw))
            {
                if (!DateOnly.TryParseExact(joinDateRaw, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var parsed))
                {
                    errors.Add(new ImportRowError(rowNumber, $"JoinDate '{joinDateRaw}' must be yyyy-MM-dd."));
                    continue;
                }

                joinDate = parsed;
            }

            _context.Employees.Add(new Employee
            {
                EmployeeCode = _settings.FormatCode(nextSequence++),
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone,
                JoinDate = joinDate,
                Status = EmployeeStatus.Active,
            });

            imported++;
        }

        if (imported > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(new BulkImportEmployeesResponse(imported, errors));
    }

    private async Task<int> GetNextSequenceAsync(CancellationToken cancellationToken)
    {
        var prefix = $"{_settings.CodePrefix}-";

        var lastCode = await _context.Employees
            .IgnoreQueryFilters()
            .Where(e => e.EmployeeCode.StartsWith(prefix))
            .OrderByDescending(e => e.EmployeeCode)
            .Select(e => e.EmployeeCode)
            .FirstOrDefaultAsync(cancellationToken);

        return lastCode is not null && int.TryParse(lastCode[prefix.Length..], out var last)
            ? last + 1
            : 1;
    }
}
