using Employee360.Application.Common.Models;
using Employee360.Application.Features.EmailTemplates;
using Employee360.Domain.Constants;
using Employee360.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>Configurable workflow email templates (FR-ADM-003).</summary>
[Route("api/v1/email-templates")]
public sealed class EmailTemplatesController : ApiControllerBase
{
    /// <summary>Lists email templates (paginated).</summary>
    [HttpGet]
    [HasPermission(Permissions.Administration.SystemConfiguration)]
    [ProducesResponseType(typeof(PagedResult<EmailTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => FromResult(await Sender.Send(new GetEmailTemplatesPagedQuery(page, pageSize), cancellationToken));

    /// <summary>Gets one email template by id.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Administration.SystemConfiguration)]
    [ProducesResponseType(typeof(EmailTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetEmailTemplateByIdQuery(id), cancellationToken));

    /// <summary>Creates an email template.</summary>
    [HttpPost]
    [HasPermission(Permissions.Administration.SystemConfiguration)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateEmailTemplateCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    /// <summary>Updates an email template.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Administration.SystemConfiguration)]
    [ProducesResponseType(typeof(EmailTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateEmailTemplateRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new UpdateEmailTemplateCommand(
                id, body.Name, body.Category, body.Description, body.Subject, body.BodyHtml, body.IsActive),
            cancellationToken));

    /// <summary>Deletes an email template.</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Administration.SystemConfiguration)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new DeleteEmailTemplateCommand(id), cancellationToken));

    /// <summary>Previews token merge for subject and body.</summary>
    [HttpPost("preview")]
    [HasPermission(Permissions.Administration.SystemConfiguration)]
    [ProducesResponseType(typeof(PreviewEmailTemplateResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Preview(
        [FromBody] PreviewEmailTemplateCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    /// <summary>Request body for template update (id from route).</summary>
    public sealed record UpdateEmailTemplateRequest(
        string Name,
        string Category,
        string Description,
        string Subject,
        string BodyHtml,
        bool IsActive);
}
