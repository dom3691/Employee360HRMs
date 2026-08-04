using Employee360.Application.Features.Recruitment.Onboarding;
using Employee360.Domain.Constants;
using Employee360.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>Onboarding checklist (FR-REC-006).</summary>
[Route("api/v1/onboarding")]
public sealed class OnboardingController : ApiControllerBase
{
    [HttpGet("tasks")]
    [HasPermission(Permissions.Recruitment.Manage)]
    [ProducesResponseType(typeof(IReadOnlyList<OnboardingTaskDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTasks(
        [FromQuery] Guid? employeeId,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetOnboardingTasksQuery(employeeId), cancellationToken));

    [HttpPost("tasks")]
    [HasPermission(Permissions.Recruitment.Manage)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignTask(
        [FromBody] AssignOnboardingTaskCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    [HttpPost("tasks/{id:guid}/complete")]
    [HasPermission(Permissions.Recruitment.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CompleteTask([FromRoute] Guid id, CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new CompleteOnboardingTaskCommand(id), cancellationToken));
}
