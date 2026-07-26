using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Employee360.Application.Features.Employees.CreateEmployee;

/// <summary>
/// Handles <see cref="CreateEmployeeCommand"/>: validates references, generates
/// the next employee code (FR-EMP-001), encrypts NIN and account number
/// (NFR-SEC-004), and persists the aggregate as Active.
/// </summary>
public sealed class CreateEmployeeHandler
    : IRequestHandler<CreateEmployeeCommand, Result<CreateEmployeeResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly EmployeeSettings _settings;

    public CreateEmployeeHandler(
        IApplicationDbContext context,
        IEncryptionService encryptionService,
        IOptions<EmployeeSettings> settings)
    {
        _context = context;
        _encryptionService = encryptionService;
        _settings = settings.Value;
    }

    /// <inheritdoc />
    public async Task<Result<CreateEmployeeResponse>> Handle(
        CreateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        var emailTaken = await _context.Employees
            .AnyAsync(e => e.Email.ToLower() == email.ToLower(), cancellationToken);

        if (emailTaken)
        {
            return Result.Failure<CreateEmployeeResponse>(
                $"An employee with email '{email}' already exists.");
        }

        if (request.DepartmentId.HasValue &&
            !await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value, cancellationToken))
        {
            return Result.Failure<CreateEmployeeResponse>("Department not found.");
        }

        if (request.PositionId.HasValue &&
            !await _context.Positions.AnyAsync(p => p.Id == request.PositionId.Value, cancellationToken))
        {
            return Result.Failure<CreateEmployeeResponse>("Position not found.");
        }

        if (request.ManagerId.HasValue &&
            !await _context.Employees.AnyAsync(e => e.Id == request.ManagerId.Value, cancellationToken))
        {
            return Result.Failure<CreateEmployeeResponse>("Manager not found.");
        }

        var employeeCode = await GenerateNextCodeAsync(cancellationToken);

        var employee = new Employee
        {
            EmployeeCode = employeeCode,
            FirstName = request.FirstName.Trim(),
            MiddleName = request.MiddleName?.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PhoneNumber = request.PhoneNumber?.Trim(),
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            MaritalStatus = request.MaritalStatus,
            Nationality = request.Nationality?.Trim(),
            NinEncrypted = string.IsNullOrEmpty(request.Nin)
                ? null
                : _encryptionService.Encrypt(request.Nin),
            Address = request.Address?.Trim(),
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            ManagerId = request.ManagerId,
            EmploymentType = request.EmploymentType,
            JoinDate = request.JoinDate,
            WorkLocation = request.WorkLocation?.Trim(),
            // PRD 7.2 acceptance criteria: created records are saved as Active.
            Status = EmployeeStatus.Active,
        };

        if (request.BankAccount is not null)
        {
            employee.BankAccount = new EmployeeBankAccount
            {
                EmployeeId = employee.Id,
                BankName = request.BankAccount.BankName.Trim(),
                AccountNumberEncrypted = _encryptionService.Encrypt(request.BankAccount.AccountNumber),
                AccountName = request.BankAccount.AccountName.Trim(),
            };
        }

        foreach (var contact in request.Contacts ?? [])
        {
            employee.Contacts.Add(new EmployeeContact
            {
                EmployeeId = employee.Id,
                Type = contact.Type,
                FullName = contact.FullName.Trim(),
                Relationship = contact.Relationship.Trim(),
                PhoneNumber = contact.PhoneNumber.Trim(),
                Address = contact.Address?.Trim(),
            });
        }

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateEmployeeResponse(employee.Id, employee.EmployeeCode));
    }

    private async Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken)
    {
        var prefix = $"{_settings.CodePrefix}-";

        // Codes are zero-padded, so lexicographic max == numeric max.
        // IgnoreQueryFilters: codes of soft-deleted employees are never reused.
        var lastCode = await _context.Employees
            .IgnoreQueryFilters()
            .Where(e => e.EmployeeCode.StartsWith(prefix))
            .OrderByDescending(e => e.EmployeeCode)
            .Select(e => e.EmployeeCode)
            .FirstOrDefaultAsync(cancellationToken);

        var nextSequence = 1;

        if (lastCode is not null &&
            int.TryParse(lastCode[prefix.Length..], out var lastSequence))
        {
            nextSequence = lastSequence + 1;
        }

        return _settings.FormatCode(nextSequence);
    }
}
