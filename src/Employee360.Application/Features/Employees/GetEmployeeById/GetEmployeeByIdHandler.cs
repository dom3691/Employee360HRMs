using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Security;
using Employee360.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Employees.GetEmployeeById;

/// <summary>
/// Handles <see cref="GetEmployeeByIdQuery"/>, decrypting then masking NIN and
/// account number so full values never leave the API (NFR-SEC-004).
/// </summary>
public sealed class GetEmployeeByIdHandler
    : IRequestHandler<GetEmployeeByIdQuery, Result<EmployeeResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;

    public GetEmployeeByIdHandler(IApplicationDbContext context, IEncryptionService encryptionService)
    {
        _context = context;
        _encryptionService = encryptionService;
    }

    /// <inheritdoc />
    public async Task<Result<EmployeeResponse>> Handle(
        GetEmployeeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .Include(e => e.BankAccount)
            .Include(e => e.Contacts)
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee is null)
        {
            return Result.Failure<EmployeeResponse>("Employee not found.");
        }

        string? ninMasked = null;
        if (!string.IsNullOrEmpty(employee.NinEncrypted))
        {
            ninMasked = Masking.Mask(_encryptionService.Decrypt(employee.NinEncrypted));
        }

        BankAccountResponse? bankAccount = null;
        if (employee.BankAccount is not null)
        {
            bankAccount = new BankAccountResponse(
                employee.BankAccount.BankName,
                Masking.Mask(_encryptionService.Decrypt(employee.BankAccount.AccountNumberEncrypted)),
                employee.BankAccount.AccountName);
        }

        var response = new EmployeeResponse(
            employee.Id,
            employee.EmployeeCode,
            employee.FirstName,
            employee.MiddleName,
            employee.LastName,
            employee.FullName,
            employee.Email,
            employee.PhoneNumber,
            employee.DateOfBirth,
            employee.Gender,
            employee.MaritalStatus,
            employee.Nationality,
            ninMasked,
            employee.Address,
            employee.DepartmentId,
            employee.Department?.Name,
            employee.PositionId,
            employee.Position?.Title,
            employee.ManagerId,
            employee.Manager?.FullName,
            employee.EmploymentType,
            employee.JoinDate,
            employee.WorkLocation,
            employee.Status,
            bankAccount,
            employee.Contacts
                .Select(c => new ContactResponse(
                    c.Id, c.Type, c.FullName, c.Relationship, c.PhoneNumber, c.Address))
                .ToList());

        return Result.Success(response);
    }
}
