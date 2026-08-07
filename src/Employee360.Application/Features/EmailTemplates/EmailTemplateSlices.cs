using System.Text.Json;
using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Application.Common.Validation;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.EmailTemplates;

public sealed record EmailTemplateDto(
    Guid Id,
    string Code,
    string Name,
    string Category,
    string Description,
    string Subject,
    string BodyHtml,
    bool IsActive,
    DateTime? LastModified,
    string? LastModifiedBy,
    IReadOnlyList<string>? Variables);

// ---------------------------------------------------------------------------
// GetEmailTemplatesPaged
// ---------------------------------------------------------------------------

/// <summary>Lists email templates (paginated).</summary>
public sealed record GetEmailTemplatesPagedQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<EmailTemplateDto>>>;

/// <summary>Input validation for <see cref="GetEmailTemplatesPagedQuery"/>.</summary>
public sealed class GetEmailTemplatesPagedValidator : AbstractValidator<GetEmailTemplatesPagedQuery>
{
    public GetEmailTemplatesPagedValidator()
    {
        RuleFor(q => q.Page).ValidPage();
        RuleFor(q => q.PageSize).ValidPageSize();
    }
}

/// <summary>Handles <see cref="GetEmailTemplatesPagedQuery"/>.</summary>
public sealed class GetEmailTemplatesPagedHandler
    : IRequestHandler<GetEmailTemplatesPagedQuery, Result<PagedResult<EmailTemplateDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetEmailTemplatesPagedHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<EmailTemplateDto>>> Handle(
        GetEmailTemplatesPagedQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.EmailTemplates.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(t => t.Code)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => Map(t))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<EmailTemplateDto>(
            items, request.Page, request.PageSize, totalCount));
    }

    internal static EmailTemplateDto Map(EmailTemplate template) =>
        new(
            template.Id,
            template.Code,
            template.Name,
            template.Category,
            template.Description,
            template.Subject,
            template.BodyHtml,
            template.IsActive,
            template.ModifiedAt ?? template.CreatedAt,
            null,
            DeserializeVariables(template.VariablesJson));

    private static IReadOnlyList<string>? DeserializeVariables(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<List<string>>(json);
    }
}

// ---------------------------------------------------------------------------
// GetEmailTemplateById
// ---------------------------------------------------------------------------

/// <summary>Gets one email template by id.</summary>
public sealed record GetEmailTemplateByIdQuery(Guid Id) : IRequest<Result<EmailTemplateDto>>;

/// <summary>Handles <see cref="GetEmailTemplateByIdQuery"/>.</summary>
public sealed class GetEmailTemplateByIdHandler
    : IRequestHandler<GetEmailTemplateByIdQuery, Result<EmailTemplateDto>>
{
    private readonly IApplicationDbContext _context;

    public GetEmailTemplateByIdHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<EmailTemplateDto>> Handle(
        GetEmailTemplateByIdQuery request,
        CancellationToken cancellationToken)
    {
        var template = await _context.EmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        return template is null
            ? Result.Failure<EmailTemplateDto>("Email template not found.")
            : Result.Success(GetEmailTemplatesPagedHandler.Map(template));
    }
}

// ---------------------------------------------------------------------------
// CreateEmailTemplate
// ---------------------------------------------------------------------------

/// <summary>Creates a configurable email template.</summary>
public sealed record CreateEmailTemplateCommand(
    string Code,
    string Name,
    string Subject,
    string BodyHtml,
    bool IsActive = true) : IRequest<Result<Guid>>;

/// <summary>Input validation for <see cref="CreateEmailTemplateCommand"/>.</summary>
public sealed class CreateEmailTemplateValidator : AbstractValidator<CreateEmailTemplateCommand>
{
    public CreateEmailTemplateValidator()
    {
        RuleFor(c => c.Code)
            .NotEmpty().MaximumLength(16)
            .Matches(@"^EML-\d{3}$").WithMessage("Code must match EML-NNN format.");

        RuleFor(c => c.Name).NotEmpty().MaximumLength(128);
        RuleFor(c => c.Subject).NotEmpty().MaximumLength(256);
        RuleFor(c => c.BodyHtml).NotEmpty();
    }
}

/// <summary>Handles <see cref="CreateEmailTemplateCommand"/>.</summary>
public sealed class CreateEmailTemplateHandler : IRequestHandler<CreateEmailTemplateCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateEmailTemplateHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(
        CreateEmailTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        if (await _context.EmailTemplates.AnyAsync(t => t.Code == code, cancellationToken))
        {
            return Result.Failure<Guid>($"An email template with code '{code}' already exists.");
        }

        var template = new EmailTemplate
        {
            Code = code,
            Name = request.Name.Trim(),
            Subject = request.Subject.Trim(),
            BodyHtml = request.BodyHtml,
            IsActive = request.IsActive,
        };

        _context.EmailTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(template.Id);
    }
}

// ---------------------------------------------------------------------------
// UpdateEmailTemplate
// ---------------------------------------------------------------------------

/// <summary>Updates an email template.</summary>
public sealed record UpdateEmailTemplateCommand(
    Guid Id,
    string Name,
    string Category,
    string Description,
    string Subject,
    string BodyHtml,
    bool IsActive) : IRequest<Result<EmailTemplateDto>>;

/// <summary>Input validation for <see cref="UpdateEmailTemplateCommand"/>.</summary>
public sealed class UpdateEmailTemplateValidator : AbstractValidator<UpdateEmailTemplateCommand>
{
    public UpdateEmailTemplateValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(128);
        RuleFor(c => c.Subject).NotEmpty().MaximumLength(256);
        RuleFor(c => c.BodyHtml).NotEmpty();
    }
}

/// <summary>Handles <see cref="UpdateEmailTemplateCommand"/>.</summary>
public sealed class UpdateEmailTemplateHandler : IRequestHandler<UpdateEmailTemplateCommand, Result<EmailTemplateDto>>
{
    private readonly IApplicationDbContext _context;

    public UpdateEmailTemplateHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<EmailTemplateDto>> Handle(
        UpdateEmailTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (template is null)
        {
            return Result.Failure<EmailTemplateDto>("Email template not found.");
        }

        template.Name = request.Name.Trim();
        template.Category = request.Category.Trim();
        template.Description = request.Description.Trim();
        template.Subject = request.Subject.Trim();
        template.BodyHtml = request.BodyHtml;
        template.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(GetEmailTemplatesPagedHandler.Map(template));
    }
}

// ---------------------------------------------------------------------------
// DeleteEmailTemplate
// ---------------------------------------------------------------------------

/// <summary>Deletes an email template (system defaults remain available).</summary>
public sealed record DeleteEmailTemplateCommand(Guid Id) : IRequest<Result>;

/// <summary>Handles <see cref="DeleteEmailTemplateCommand"/>.</summary>
public sealed class DeleteEmailTemplateHandler : IRequestHandler<DeleteEmailTemplateCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteEmailTemplateHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteEmailTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (template is null)
        {
            return Result.Failure("Email template not found.");
        }

        _context.EmailTemplates.Remove(template);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

// ---------------------------------------------------------------------------
// PreviewEmailTemplate (token merge)
// ---------------------------------------------------------------------------

/// <summary>Previews a template with sample token values.</summary>
public sealed record PreviewEmailTemplateCommand(
    string Subject,
    string BodyHtml,
    IReadOnlyDictionary<string, string> Tokens) : IRequest<Result<PreviewEmailTemplateResult>>;

/// <summary>Preview output with merged subject and body.</summary>
public sealed record PreviewEmailTemplateResult(string Subject, string BodyHtml);

/// <summary>Input validation for <see cref="PreviewEmailTemplateCommand"/>.</summary>
public sealed class PreviewEmailTemplateValidator : AbstractValidator<PreviewEmailTemplateCommand>
{
    public PreviewEmailTemplateValidator()
    {
        RuleFor(c => c.Subject).NotEmpty();
        RuleFor(c => c.BodyHtml).NotEmpty();
    }
}

/// <summary>Handles <see cref="PreviewEmailTemplateCommand"/>.</summary>
public sealed class PreviewEmailTemplateHandler
    : IRequestHandler<PreviewEmailTemplateCommand, Result<PreviewEmailTemplateResult>>
{
    private readonly IEmailTemplateService _templateService;

    public PreviewEmailTemplateHandler(IEmailTemplateService templateService)
    {
        _templateService = templateService;
    }

    /// <inheritdoc />
    public Task<Result<PreviewEmailTemplateResult>> Handle(
        PreviewEmailTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var result = new PreviewEmailTemplateResult(
            _templateService.MergeTokens(request.Subject, request.Tokens),
            _templateService.MergeTokens(request.BodyHtml, request.Tokens));

        return Task.FromResult(Result.Success(result));
    }
}
