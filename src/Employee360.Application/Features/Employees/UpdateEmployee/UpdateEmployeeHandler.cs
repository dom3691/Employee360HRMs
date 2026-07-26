using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Employees.UpdateEmployee;

/// <summary>
/// Handles <see cref="UpdateEmployeeCommand"/>: reference validation including
/// reporting-cycle prevention, PII re-encryption, and bank account upsert.
/// All changes are captured by the audit interceptor (FR-EMP-006).
/// </summary>
public sealed class UpdateEmployeeHandler : IRequestHandler<UpdateEmployeeCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly IManagerScopeService _managerScopeService;

    public UpdateEmployeeHandler(
        IApplicationDbContext context,
        IEncryptionService encryptionService,
        IManagerScopeService managerScopeService)
    {
        _context = context;
        _encryptionService = encryptionService;
        _managerScopeService = managerScopeService;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .Include(e => e.BankAccount)
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee is null)
        {
            return Result.Failure("Employee not found.");
        }

        if (request.DepartmentId.HasValue &&
            !await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value, cancellationToken))
        {
            return Result.Failure("Department not found.");
        }

        if (request.PositionId.HasValue &&
            !await _context.Positions.AnyAsync(p => p.Id == request.PositionId.Value, cancellationToken))
        {
            return Result.Failure("Position not found.");
        }

        if (request.ManagerId.HasValue && request.ManagerId != employee.ManagerId)
        {
            if (request.ManagerId.Value == employee.Id)
            {
                return Result.Failure("An employee cannot be their own manager.");
            }

            if (!await _context.Employees.AnyAsync(e => e.Id == request.ManagerId.Value, cancellationToken))
            {
                return Result.Failure("Manager not found.");
            }

            // The new manager must not report (directly or indirectly) to this
            // employee, otherwise the hierarchy would contain a cycle.
            if (await _managerScopeService.IsManagerOfAsync(
                    employee.Id, request.ManagerId.Value, cancellationToken))
            {
                return Result.Failure(
                    "Invalid reporting line: the selected manager reports to this employee.");
            }
        }

        employee.FirstName = request.FirstName.Trim();
        employee.MiddleName = request.MiddleName?.Trim();
        employee.LastName = request.LastName.Trim();
        employee.PhoneNumber = request.PhoneNumber?.Trim();
        employee.DateOfBirth = request.DateOfBirth;
        employee.Gender = request.Gender;
        employee.MaritalStatus = request.MaritalStatus;
        employee.Nationality = request.Nationality?.Trim();
        employee.Address = request.Address?.Trim();
        employee.DepartmentId = request.DepartmentId;
        employee.PositionId = request.PositionId;
        employee.ManagerId = request.ManagerId;
        employee.EmploymentType = request.EmploymentType;
        employee.JoinDate = request.JoinDate;
        employee.WorkLocation = request.WorkLocation?.Trim();

        if (!string.IsNullOrEmpty(request.Nin))
        {
            employee.NinEncrypted = _encryptionService.Encrypt(request.Nin);
        }

        if (request.BankAccount is not null)
        {
            if (employee.BankAccount is null)
            {
                employee.BankAccount = new EmployeeBankAccount { EmployeeId = employee.Id };
                _context.EmployeeBankAccounts.Add(employee.BankAccount);
            }

            employee.BankAccount.BankName = request.BankAccount.BankName.Trim();
            employee.BankAccount.AccountNumberEncrypted =
                _encryptionService.Encrypt(request.BankAccount.AccountNumber);
            employee.BankAccount.AccountName = request.BankAccount.AccountName.Trim();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
