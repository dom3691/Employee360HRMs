using Employee360.Application.Common.Models;
using Employee360.Application.Features.Departments.CreateDepartment;
using Employee360.Application.Features.Departments.DeleteDepartment;
using Employee360.Application.Features.Departments.GetDepartments;
using Employee360.Application.Features.Departments.UpdateDepartment;
using Employee360.Domain.Constants;
using Employee360.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>Department management endpoints (FR-EMP-009).</summary>
[Route("api/v1/departments")]
public sealed class DepartmentsController : ApiControllerBase
{
    /// <summary>Lists departments with search and pagination.</summary>
    [HttpGet]
    [HasPermission(Permissions.Departments.View)]
    [ProducesResponseType(typeof(PagedResult<DepartmentListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
        => FromResult(await Sender.Send(
            new GetDepartmentsPagedQuery(page, pageSize, search), cancellationToken));

    /// <summary>Returns the department hierarchy tree.</summary>
    [HttpGet("tree")]
    [HasPermission(Permissions.Departments.View)]
    [ProducesResponseType(typeof(IReadOnlyList<DepartmentTreeNode>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Tree(CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetDepartmentTreeQuery(), cancellationToken));

    /// <summary>Gets one department by id.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Departments.View)]
    [ProducesResponseType(typeof(DepartmentListItem), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetDepartmentByIdQuery(id), cancellationToken));

    /// <summary>Creates a department.</summary>
    [HttpPost]
    [HasPermission(Permissions.Departments.Manage)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDepartmentCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    /// <summary>Updates a department (circular hierarchies rejected).</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Departments.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateDepartmentRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new UpdateDepartmentCommand(id, body.Name, body.Code, body.ParentDepartmentId, body.HeadEmployeeId),
            cancellationToken));

    /// <summary>Soft-deletes a department (blocked while children or employees exist).</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Departments.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new DeleteDepartmentCommand(id), cancellationToken));

    /// <summary>Request body for <see cref="Update"/> (id from route).</summary>
    public sealed record UpdateDepartmentRequest(
        string Name,
        string Code,
        Guid? ParentDepartmentId,
        Guid? HeadEmployeeId);
}
