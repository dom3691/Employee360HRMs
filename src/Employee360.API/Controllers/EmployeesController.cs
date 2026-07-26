using Employee360.Application.Common.Models;
using Employee360.Application.Features.Employees.BulkImportEmployees;
using Employee360.Application.Features.Employees.CreateEmployee;
using Employee360.Application.Features.Employees.DeactivateEmployee;
using Employee360.Application.Features.Employees.GetEmployeeById;
using Employee360.Application.Features.Employees.GetEmployeesPaged;
using Employee360.Application.Features.Employees.GetOrgChart;
using Employee360.Application.Features.Employees.UpdateEmployee;
using Employee360.Application.Features.Employees.UploadEmployeeDocument;
using Employee360.Domain.Constants;
using Employee360.Domain.Enums;
using Employee360.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>Employee management endpoints (FR-EMP-001..014, Appendix F).</summary>
[Route("api/v1/employees")]
public sealed class EmployeesController : ApiControllerBase
{
    /// <summary>Lists employees with filtering, sorting, and pagination.</summary>
    [HttpGet]
    [HasPermission(Permissions.Employees.ViewAll)]
    [ProducesResponseType(typeof(PagedResult<EmployeeListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] EmployeeStatus? status = null,
        [FromQuery] string? search = null,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDirection = "asc",
        CancellationToken cancellationToken = default)
        => FromResult(await Sender.Send(
            new GetEmployeesPagedQuery(page, pageSize, departmentId, status, search, sortBy, sortDirection),
            cancellationToken));

    /// <summary>Creates an employee with an auto-generated employee code.</summary>
    [HttpPost]
    [HasPermission(Permissions.Employees.Create)]
    [ProducesResponseType(typeof(CreateEmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateEmployeeCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    /// <summary>Gets an employee's full profile (PII masked).</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Employees.ViewAll)]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetEmployeeByIdQuery(id), cancellationToken));

    /// <summary>Updates an employee's master data.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Employees.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateEmployeeRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(body.ToCommand(id), cancellationToken));

    /// <summary>Deactivates an employee (Suspended / Resigned / Terminated) and disables their login.</summary>
    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(Permissions.Employees.Deactivate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Deactivate(
        [FromRoute] Guid id,
        [FromBody] DeactivateEmployeeRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new DeactivateEmployeeCommand(id, body.NewStatus, body.Reason), cancellationToken));

    /// <summary>Returns the organization chart tree.</summary>
    [HttpGet("org-chart")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<OrgChartNode>), StatusCodes.Status200OK)]
    public async Task<IActionResult> OrgChart(CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetOrgChartQuery(), cancellationToken));

    /// <summary>Uploads an employee document (PDF/JPG/PNG, max 10 MB).</summary>
    [HttpPost("{id:guid}/documents")]
    [HasPermission(Permissions.Documents.ManageAll)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadDocument(
        [FromRoute] Guid id,
        IFormFile file,
        [FromForm] DocumentCategory category,
        [FromForm] DateOnly? expiryDate,
        CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        var command = new UploadEmployeeDocumentCommand(
            id, file.FileName, file.ContentType, stream.ToArray(), category, expiryDate);

        return FromResult(await Sender.Send(command, cancellationToken));
    }

    /// <summary>Bulk-imports employees from a CSV file (header: FirstName,LastName,Email,PhoneNumber,JoinDate).</summary>
    [HttpPost("bulk-import")]
    [HasPermission(Permissions.Employees.Create)]
    [ProducesResponseType(typeof(BulkImportEmployeesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkImport(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        return FromResult(await Sender.Send(
            new BulkImportEmployeesCommand(stream.ToArray()), cancellationToken));
    }

    /// <summary>Request body for <see cref="Deactivate"/>.</summary>
    public sealed record DeactivateEmployeeRequest(EmployeeStatus NewStatus, string Reason);

    /// <summary>Request body for <see cref="Update"/> (id comes from the route).</summary>
    public sealed record UpdateEmployeeRequest(
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
        BankAccountInput? BankAccount)
    {
        /// <summary>Combines the route id with the body into the command.</summary>
        public UpdateEmployeeCommand ToCommand(Guid id) => new(
            id, FirstName, MiddleName, LastName, PhoneNumber, DateOfBirth, Gender,
            MaritalStatus, Nationality, Nin, Address, DepartmentId, PositionId,
            ManagerId, EmploymentType, JoinDate, WorkLocation, BankAccount);
    }
}
