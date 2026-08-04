using System.Text.RegularExpressions;
using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Constants;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Common.Services;

/// <summary>
/// Default <see cref="IEmailTemplateService"/>: token merge plus DB-backed templates
/// with PRD Appendix I fallbacks (FR-ADM-003).
/// </summary>
public sealed partial class EmailTemplateService : IEmailTemplateService
{
    private static readonly Regex TokenPattern = TokenRegex();

    private readonly IApplicationDbContext _context;

    public EmailTemplateService(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public string MergeTokens(string template, IReadOnlyDictionary<string, string> tokens)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template;
        }

        return TokenPattern.Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            return tokens.TryGetValue(key, out var value) ? value : match.Value;
        });
    }

    /// <inheritdoc />
    public async Task<(string Subject, string BodyHtml)> RenderAsync(
        string code,
        IReadOnlyDictionary<string, string> tokens,
        CancellationToken cancellationToken = default)
    {
        var template = await _context.EmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Code == code && t.IsActive, cancellationToken);

        if (template is not null)
        {
            return (
                MergeTokens(template.Subject, tokens),
                MergeTokens(template.BodyHtml, tokens));
        }

        var defaults = GetBuiltInDefaults(code);
        return (
            MergeTokens(defaults.Subject, tokens),
            MergeTokens(defaults.BodyHtml, tokens));
    }

    public static (string Subject, string BodyHtml) GetBuiltInDefaults(string code) =>
        code switch
        {
            EmailTemplateCodes.LeaveSubmitted => (
                "Leave Request from {EmployeeName} — Action Required",
                """
                <p>{EmployeeName} has requested <strong>{Days}</strong> day(s) of leave
                from <strong>{StartDate}</strong> to <strong>{EndDate}</strong>.</p>
                <p>Reason: {Reason}</p>
                <p>Log in to Employee360 to approve or reject this request.</p>
                """),
            EmailTemplateCodes.LeaveApproved => (
                "Your Leave Request has been Approved",
                """
                <p>Your leave request for <strong>{Days}</strong> day(s)
                ({StartDate} – {EndDate}) has been <strong>Approved</strong>.</p>
                {CommentsBlock}
                """),
            EmailTemplateCodes.LeaveRejected => (
                "Your Leave Request has been Declined",
                """
                <p>Your leave request for <strong>{Days}</strong> day(s)
                ({StartDate} – {EndDate}) has been <strong>Declined</strong>.</p>
                {CommentsBlock}
                """),
            EmailTemplateCodes.LeaveEscalated => (
                "Escalated: Pending Leave Approval for {EmployeeName}",
                """
                <p>A leave request from {EmployeeName}
                ({StartDate} – {EndDate}, {Days} day(s)) has exceeded the approval SLA
                and is now assigned to you.</p>
                """),
            EmailTemplateCodes.WelcomeAccountCreated => (
                "Welcome to Employee360 — Set Up Your Account",
                """
                <p>Welcome {EmployeeName},</p>
                <p>Your Employee360 account has been created. Use the link below to set your password.</p>
                <p><a href="{SetupUrl}">Set up your account</a></p>
                """),
            EmailTemplateCodes.PasswordReset => (
                "Employee360 Password Reset Request",
                """
                <p>We received a request to reset your Employee360 password.</p>
                <p>Your reset code is: <strong>{ResetToken}</strong></p>
                <p>This code expires in {ExpiryMinutes} minutes.</p>
                """),
            EmailTemplateCodes.PayslipAvailable => (
                "Your Payslip for {Period} is Ready",
                """
                <p>Your payslip for <strong>{Period}</strong> is now available.</p>
                <p>Log in to Employee360 to view and download your payslip.</p>
                """),
            EmailTemplateCodes.PayrollPendingApproval => (
                "Payroll Run {Period} — Approval Required",
                """
                <p>Payroll run <strong>{Period}</strong> is ready for your approval.</p>
                <p>Log in to Employee360 to review and approve the payroll run.</p>
                """),
            EmailTemplateCodes.ProfileChangeApproved => (
                "Your Profile Update has been Approved",
                """
                <p>Your profile update request has been <strong>approved</strong>.</p>
                <p>Log in to Employee360 to view your updated profile.</p>
                """),
            EmailTemplateCodes.OnboardingTaskAssigned => (
                "Onboarding Task Assigned: {TaskName}",
                """
                <p>You have been assigned an onboarding task: <strong>{TaskName}</strong>.</p>
                <p>Due date: {DueDate}</p>
                """),
            EmailTemplateCodes.OfferLetter => (
                "Offer of Employment — {JobTitle}",
                """
                <p>Dear {CandidateName},</p>
                <p>We are pleased to offer you the position of <strong>{JobTitle}</strong> in {Department}.</p>
                <p>Proposed start date: {StartDate}</p>
                <p>Offer date: {OfferDate}</p>
                <p>We look forward to welcoming you to the team.</p>
                """),
            _ => (
                "Employee360 Notification",
                "<p>You have a new notification from Employee360.</p>"),
        };

    [GeneratedRegex(@"\{(\w+)\}", RegexOptions.Compiled)]
    private static partial Regex TokenRegex();
}
