using Employee360.Application.Common.Models;
using Employee360.Application.Features.Employees.CreateEmployee;
using Employee360.Application.Features.Recruitment.Candidates;
using Employee360.Domain.Constants;
using Employee360.Domain.Enums;
using Employee360.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>Candidate pipeline management (FR-REC-002..005).</summary>
[Route("api/v1/candidates")]
public sealed class CandidatesController : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Recruitment.Manage)]
    [ProducesResponseType(typeof(PagedResult<CandidateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? jobPostingId = null,
        [FromQuery] CandidateStage? stage = null,
        CancellationToken cancellationToken = default)
        => FromResult(await Sender.Send(
            new GetCandidatesQuery(page, pageSize, jobPostingId, stage), cancellationToken));

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Recruitment.Manage)]
    [ProducesResponseType(typeof(CandidateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetCandidateByIdQuery(id), cancellationToken));

    /// <summary>Public job application endpoint.</summary>
    [HttpPost("apply")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> Apply(
        [FromBody] ApplyForJobCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    [HttpPost("{id:guid}/stage")]
    [HasPermission(Permissions.Recruitment.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> TransitionStage(
        [FromRoute] Guid id,
        [FromBody] TransitionStageRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new TransitionCandidateStageCommand(id, body.Stage), cancellationToken));

    [HttpPost("{id:guid}/resume")]
    [HasPermission(Permissions.Recruitment.Manage)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadResume(
        [FromRoute] Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new ProblemDetails { Title = "File is empty." });
        }

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        return FromResult(await Sender.Send(
            new UploadCandidateResumeCommand(
                id, file.FileName, file.ContentType, stream.ToArray()),
            cancellationToken));
    }

    [HttpGet("{id:guid}/offer-letter")]
    [HasPermission(Permissions.Recruitment.Manage)]
    [ProducesResponseType(typeof(OfferLetterDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> OfferLetter([FromRoute] Guid id, CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GenerateOfferLetterQuery(id), cancellationToken));

    [HttpPost("{id:guid}/convert-to-employee")]
    [HasPermission(Permissions.Recruitment.Manage)]
    [ProducesResponseType(typeof(CreateEmployeeResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConvertToEmployee(
        [FromRoute] Guid id,
        [FromBody] ConvertToEmployeeRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new ConvertCandidateToEmployeeCommand(id, body.PositionId, body.ManagerId, body.JoinDate),
            cancellationToken));

    public sealed record TransitionStageRequest(CandidateStage Stage);

    public sealed record ConvertToEmployeeRequest(
        Guid? PositionId,
        Guid? ManagerId,
        DateOnly? JoinDate);
}
