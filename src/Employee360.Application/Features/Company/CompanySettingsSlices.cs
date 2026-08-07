using System.Text.Json;
using Employee360.Application.Common.Interfaces;
using Employee360.Application.Features.Company;
using Employee360.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CompanyEntity = Employee360.Domain.Entities.Company;

namespace Employee360.Application.Features.Company;

public sealed record WorkingCalendarDto(
    IReadOnlyList<string> WorkingDays,
    string StartTime,
    string EndTime,
    int BreakMinutes,
    string Timezone,
    string DefaultCurrency);

public sealed record EmailConfigurationDto(
    string DisplayName,
    string FromEmail,
    string SmtpHost,
    int SmtpPort,
    string SmtpUsername,
    string? SmtpPassword);

public sealed record GetWorkingCalendarQuery : IRequest<Result<WorkingCalendarDto>>;

public sealed class GetWorkingCalendarHandler : IRequestHandler<GetWorkingCalendarQuery, Result<WorkingCalendarDto>>
{
    private readonly IApplicationDbContext _context;

    public GetWorkingCalendarHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<WorkingCalendarDto>> Handle(
        GetWorkingCalendarQuery request,
        CancellationToken cancellationToken)
    {
        var company = await CompanySettingsHelpers.GetCompanyAsync(_context, cancellationToken);
        if (company is null)
        {
            return Result.Failure<WorkingCalendarDto>("Company profile has not been initialized.");
        }

        return Result.Success(DeserializeCalendar(company));
    }

    internal static WorkingCalendarDto DeserializeCalendar(CompanyEntity company)
    {
        if (!string.IsNullOrWhiteSpace(company.WorkingCalendarJson))
        {
            var stored = JsonSerializer.Deserialize<WorkingCalendarDto>(company.WorkingCalendarJson);
            if (stored is not null)
            {
                return stored;
            }
        }

        return new WorkingCalendarDto(
            ["Mon", "Tue", "Wed", "Thu", "Fri"],
            "08:00",
            "17:00",
            60,
            "Africa/Lagos",
            company.DefaultCurrency);
    }
}

public sealed record UpdateWorkingCalendarCommand(
    IReadOnlyList<string> WorkingDays,
    string StartTime,
    string EndTime,
    int BreakMinutes,
    string Timezone,
    string DefaultCurrency) : IRequest<Result<WorkingCalendarDto>>;

public sealed class UpdateWorkingCalendarValidator : AbstractValidator<UpdateWorkingCalendarCommand>
{
    public UpdateWorkingCalendarValidator()
    {
        RuleFor(c => c.WorkingDays).NotEmpty();
        RuleFor(c => c.StartTime).NotEmpty();
        RuleFor(c => c.EndTime).NotEmpty();
        RuleFor(c => c.Timezone).NotEmpty();
        RuleFor(c => c.DefaultCurrency).Length(3);
    }
}

public sealed class UpdateWorkingCalendarHandler : IRequestHandler<UpdateWorkingCalendarCommand, Result<WorkingCalendarDto>>
{
    private readonly IApplicationDbContext _context;

    public UpdateWorkingCalendarHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<WorkingCalendarDto>> Handle(
        UpdateWorkingCalendarCommand request,
        CancellationToken cancellationToken)
    {
        var company = await CompanySettingsHelpers.GetOrCreateCompanyAsync(_context, cancellationToken);
        var dto = new WorkingCalendarDto(
            request.WorkingDays,
            request.StartTime.Trim(),
            request.EndTime.Trim(),
            request.BreakMinutes,
            request.Timezone.Trim(),
            request.DefaultCurrency.Trim().ToUpperInvariant());

        company.WorkingCalendarJson = JsonSerializer.Serialize(dto);
        company.DefaultCurrency = dto.DefaultCurrency;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success(dto);
    }
}

public sealed record GetEmailConfigurationQuery : IRequest<Result<EmailConfigurationDto>>;

public sealed class GetEmailConfigurationHandler : IRequestHandler<GetEmailConfigurationQuery, Result<EmailConfigurationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEncryptionService _encryption;

    public GetEmailConfigurationHandler(IApplicationDbContext context, IEncryptionService encryption)
    {
        _context = context;
        _encryption = encryption;
    }

    public async Task<Result<EmailConfigurationDto>> Handle(
        GetEmailConfigurationQuery request,
        CancellationToken cancellationToken)
    {
        var company = await CompanySettingsHelpers.GetCompanyAsync(_context, cancellationToken);
        if (company is null)
        {
            return Result.Failure<EmailConfigurationDto>("Company profile has not been initialized.");
        }

        if (string.IsNullOrWhiteSpace(company.EmailConfigurationJson))
        {
            return Result.Success(new EmailConfigurationDto(
                company.Name, company.Email ?? string.Empty,
                string.Empty, 587, string.Empty, null));
        }

        var stored = EmailConfigurationStorage.Deserialize(company.EmailConfigurationJson, _encryption);
        if (stored is null)
        {
            return Result.Failure<EmailConfigurationDto>("Email configuration is invalid.");
        }

        return Result.Success(stored with { SmtpPassword = MaskPassword(stored.SmtpPassword) });
    }

    private static string? MaskPassword(string? password)
        => string.IsNullOrEmpty(password) ? null : "********";
}

public sealed record UpdateEmailConfigurationCommand(
    string DisplayName,
    string FromEmail,
    string SmtpHost,
    int SmtpPort,
    string SmtpUsername,
    string? SmtpPassword) : IRequest<Result<EmailConfigurationDto>>;

public sealed class UpdateEmailConfigurationValidator : AbstractValidator<UpdateEmailConfigurationCommand>
{
    public UpdateEmailConfigurationValidator()
    {
        RuleFor(c => c.DisplayName).NotEmpty();
        RuleFor(c => c.FromEmail).NotEmpty().EmailAddress();
        RuleFor(c => c.SmtpHost).NotEmpty();
        RuleFor(c => c.SmtpPort).InclusiveBetween(1, 65535);
        RuleFor(c => c.SmtpUsername).NotEmpty();
    }
}

public sealed class UpdateEmailConfigurationHandler
    : IRequestHandler<UpdateEmailConfigurationCommand, Result<EmailConfigurationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEncryptionService _encryption;

    public UpdateEmailConfigurationHandler(IApplicationDbContext context, IEncryptionService encryption)
    {
        _context = context;
        _encryption = encryption;
    }

    public async Task<Result<EmailConfigurationDto>> Handle(
        UpdateEmailConfigurationCommand request,
        CancellationToken cancellationToken)
    {
        var company = await CompanySettingsHelpers.GetOrCreateCompanyAsync(_context, cancellationToken);

        string? password = request.SmtpPassword;
        if (string.IsNullOrWhiteSpace(password) && !string.IsNullOrWhiteSpace(company.EmailConfigurationJson))
        {
            var existing = EmailConfigurationStorage.Deserialize(company.EmailConfigurationJson, _encryption);
            password = existing?.SmtpPassword;
        }
        else if (!string.IsNullOrWhiteSpace(password))
        {
            password = EmailConfigurationStorage.EncryptPassword(password, _encryption);
        }

        var dto = new EmailConfigurationDto(
            request.DisplayName.Trim(),
            request.FromEmail.Trim(),
            request.SmtpHost.Trim(),
            request.SmtpPort,
            request.SmtpUsername.Trim(),
            password);

        company.EmailConfigurationJson = EmailConfigurationStorage.Serialize(dto);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(dto with { SmtpPassword = null });
    }
}

public sealed record TestEmailConfigurationCommand(string? ToEmail) : IRequest<Result>;

public sealed class TestEmailConfigurationHandler : IRequestHandler<TestEmailConfigurationCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IEncryptionService _encryption;
    private readonly IEmailService _emailService;

    public TestEmailConfigurationHandler(
        IApplicationDbContext context,
        IEncryptionService encryption,
        IEmailService emailService)
    {
        _context = context;
        _encryption = encryption;
        _emailService = emailService;
    }

    public async Task<Result> Handle(TestEmailConfigurationCommand request, CancellationToken cancellationToken)
    {
        var company = await CompanySettingsHelpers.GetCompanyAsync(_context, cancellationToken);
        if (company is null || string.IsNullOrWhiteSpace(company.EmailConfigurationJson))
        {
            return Result.Failure("Email configuration has not been set up.");
        }

        var config = EmailConfigurationStorage.Deserialize(company.EmailConfigurationJson, _encryption);
        if (config is null || string.IsNullOrWhiteSpace(config.SmtpHost) || string.IsNullOrWhiteSpace(config.FromEmail))
        {
            return Result.Failure("SMTP host and from address must be configured before sending a test email.");
        }

        var recipient = string.IsNullOrWhiteSpace(request.ToEmail)
            ? config.FromEmail
            : request.ToEmail.Trim();

        await _emailService.SendAsync(
            recipient,
            "Employee360 email configuration test",
            "<p>This is a test message from Employee360 to verify outbound email delivery.</p>",
            cancellationToken);

        return Result.Success();
    }
}

internal static class EmailConfigurationStorage
{
    internal static string Serialize(EmailConfigurationDto dto)
        => JsonSerializer.Serialize(dto);

    internal static EmailConfigurationDto? Deserialize(string json, IEncryptionService encryption)
    {
        var stored = JsonSerializer.Deserialize<EmailConfigurationDto>(json);
        if (stored is null)
        {
            return null;
        }

        return stored with { SmtpPassword = DecryptPassword(stored.SmtpPassword, encryption) };
    }

    internal static string? EncryptPassword(string password, IEncryptionService encryption)
        => encryption.Encrypt(password);

    internal static string? DecryptPassword(string? value, IEncryptionService encryption)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        try
        {
            return encryption.Decrypt(value);
        }
        catch (FormatException)
        {
            return value;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return value;
        }
    }
}

internal static class CompanySettingsHelpers
{
    internal static async Task<CompanyEntity?> GetCompanyAsync(
        IApplicationDbContext context,
        CancellationToken cancellationToken)
        => await context.Companies
            .FirstOrDefaultAsync(c => c.Id == CompanyEntity.DefaultId, cancellationToken);

    internal static async Task<CompanyEntity> GetOrCreateCompanyAsync(
        IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var company = await GetCompanyAsync(context, cancellationToken);
        if (company is not null)
        {
            return company;
        }

        company = new CompanyEntity { Id = CompanyEntity.DefaultId, Name = "Company" };
        context.Companies.Add(company);
        return company;
    }
}
