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
    [HttpGet]
    [HasPermission(Permissions.Grades.View)]
    [ProducesResponseType(typeof(PagedResult<GradeListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
        => FromResult(await Sender.Send(new GetGradesPagedQuery(page, pageSize, search), cancellationToken));

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Grades.View)]
    [ProducesResponseType(typeof(GradeListItem), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetGradeByIdQuery(id), cancellationToken));

    [HttpPost]
    [HasPermission(Permissions.Grades.Manage)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(
        [FromBody] CreateGradeCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Grades.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpsertGradeRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new UpdateGradeCommand(
                id, body.Title, body.Code, body.LevelRank, body.Description, body.SalaryMin, body.SalaryMax),
            cancellationToken));

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Grades.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new DeleteGradeCommand(id), cancellationToken));

    public sealed record UpsertGradeRequest(
        string Title,
        string Code,
        string LevelRank,
        string? Description,
        decimal SalaryMin,
        decimal SalaryMax);
}
