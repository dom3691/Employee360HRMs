using Employee360.Application.Common.Models;
using Employee360.Application.Features.Grades.CreateGrade;
using Employee360.Application.Features.Grades.DeleteGrade;
using Employee360.Application.Features.Grades.GetGrades;
using Employee360.Application.Features.Grades.UpdateGrade;
using Employee360.Domain.Constants;
using Employee360.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>Salary grade management endpoints.</summary>
[Route("api/v1/grades")]
public sealed class GradesController : ApiControllerBase
{
    /// <summary>Lists grades ordered by level.</summary>
    [HttpGet]
    [HasPermission(Permissions.Grades.View)]
    [ProducesResponseType(typeof(PagedResult<GradeListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => FromResult(await Sender.Send(new GetGradesPagedQuery(page, pageSize), cancellationToken));

    /// <summary>Gets one grade by id.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Grades.View)]
    [ProducesResponseType(typeof(GradeListItem), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetGradeByIdQuery(id), cancellationToken));

    /// <summary>Creates a grade.</summary>
    [HttpPost]
    [HasPermission(Permissions.Grades.Manage)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateGradeCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    /// <summary>Updates a grade.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Grades.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateGradeRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new UpdateGradeCommand(id, body.Name, body.Level, body.MinSalary, body.MaxSalary),
            cancellationToken));

    /// <summary>Soft-deletes a grade (blocked while positions reference it).</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Grades.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new DeleteGradeCommand(id), cancellationToken));

    /// <summary>Request body for <see cref="Update"/> (id from route).</summary>
    public sealed record UpdateGradeRequest(
        string Name,
        int Level,
        decimal MinSalary,
        decimal MaxSalary);
}
