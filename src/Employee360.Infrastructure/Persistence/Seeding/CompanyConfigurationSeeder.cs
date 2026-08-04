using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Services;
using Employee360.Domain.Constants;
using Employee360.Domain.Entities;
using Employee360.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Employee360.Infrastructure.Persistence.Seeding;

/// <summary>
/// Idempotent seeder for the singleton company profile and default email templates
/// (PRD FR-ADM-001, Appendix I EML-001..010).
/// </summary>
public sealed class CompanyConfigurationSeeder : IDataSeeder
{
    private readonly Employee360DbContext _context;
    private readonly ILogger<CompanyConfigurationSeeder> _logger;

    public CompanyConfigurationSeeder(
        Employee360DbContext context,
        ILogger<CompanyConfigurationSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedCompanyAsync(cancellationToken);
        await SeedEmailTemplatesAsync(cancellationToken);

        _logger.LogInformation("Company profile and email template seed data applied");
    }

    private async Task SeedCompanyAsync(CancellationToken cancellationToken)
    {
        if (await _context.Companies.AnyAsync(cancellationToken))
        {
            return;
        }

        _context.Companies.Add(new Company
        {
            Id = Company.DefaultId,
            Name = "Employee360 Demo Company Ltd",
            DefaultCurrency = "NGN",
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedEmailTemplatesAsync(CancellationToken cancellationToken)
    {
        var existing = await _context.EmailTemplates
            .Select(t => t.Code)
            .ToListAsync(cancellationToken);
        var existingSet = existing.ToHashSet(StringComparer.Ordinal);

        var missing = DefaultTemplates()
            .Where(t => !existingSet.Contains(t.Code))
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        await _context.EmailTemplates.AddRangeAsync(missing, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<EmailTemplate> DefaultTemplates()
    {
        foreach (var code in EmailTemplateCodes.All)
        {
            var defaults = EmailTemplateService.GetBuiltInDefaults(code);
            yield return new EmailTemplate
            {
                Code = code,
                Name = TemplateName(code),
                Subject = defaults.Subject,
                BodyHtml = defaults.BodyHtml,
                IsActive = true,
            };
        }
    }

    private static string TemplateName(string code) =>
        code switch
        {
            EmailTemplateCodes.LeaveSubmitted => "Leave submitted",
            EmailTemplateCodes.LeaveApproved => "Leave approved",
            EmailTemplateCodes.LeaveRejected => "Leave rejected",
            EmailTemplateCodes.LeaveEscalated => "Leave escalated",
            EmailTemplateCodes.WelcomeAccountCreated => "Welcome / account created",
            EmailTemplateCodes.PasswordReset => "Password reset",
            EmailTemplateCodes.PayslipAvailable => "Payslip available",
            EmailTemplateCodes.PayrollPendingApproval => "Payroll pending approval",
            EmailTemplateCodes.ProfileChangeApproved => "Profile change approved",
            EmailTemplateCodes.OnboardingTaskAssigned => "Onboarding task assigned",
            EmailTemplateCodes.OfferLetter => "Offer letter",
            _ => code,
        };
}
