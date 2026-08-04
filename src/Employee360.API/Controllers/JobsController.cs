using Employee360.Application.Common.Models;
using Employee360.Application.Features.Recruitment.JobPostings;
using Employee360.Domain.Constants;
using Employee360.Domain.Enums;
using Employee360.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>Job posting management (FR-REC-001).</summary>
[Route("api/v1/jobs")]
public sealed class JobsController : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Recruitment.Manage)]
    [ProducesResponseType(typeof(PagedResult<JobPostingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] JobPostingStatus? status = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
        => FromResult(await Sender.Send(new GetJobPostingsQuery(page, pageSize, status, search), cancellationToken));

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Recruitment.Manage)]
    [ProducesResponseType(typeof(JobPostingDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetJobPostingByIdQuery(id), cancellationToken));

    [HttpPost]
    [HasPermission(Permissions.Recruitment.Manage)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(
        [FromBody] CreateJobPostingCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Recruitment.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateJobPostingRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new UpdateJobPostingCommand(id, body.Title, body.DepartmentId, body.Description, body.ClosingDate),
            cancellationToken));

    [HttpPost("{id:guid}/publish")]
    [HasPermission(Permissions.Recruitment.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Publish([FromRoute] Guid id, CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new PublishJobPostingCommand(id), cancellationToken));

    [HttpPost("{id:guid}/close")]
    [HasPermission(Permissions.Recruitment.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Close([FromRoute] Guid id, CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new CloseJobPostingCommand(id), cancellationToken));

    public sealed record UpdateJobPostingRequest(
        string Title,
        Guid? DepartmentId,
        string Description,
        DateOnly? ClosingDate);
}

/// <summary>Public careers page — published jobs (FR-REC-001).</summary>
[Route("api/v1/careers")]
[AllowAnonymous]
public sealed class CareersController : ApiControllerBase
{
    [HttpGet("jobs")]
    [ProducesResponseType(typeof(IReadOnlyList<JobPostingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PublishedJobs(CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetPublishedJobsQuery(), cancellationToken));
}
