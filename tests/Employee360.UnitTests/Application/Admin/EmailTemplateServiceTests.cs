using Employee360.Application.Common.Services;
using Employee360.Application.Features.AuditLogs;
using Employee360.Domain.Entities;
using FluentAssertions;

namespace Employee360.UnitTests.Application.Admin;

/// <summary>Tests for email template token merging (FR-ADM-003).</summary>
public class EmailTemplateServiceTests
{
    [Fact]
    public void MergeTokens_ReplacesKnownTokens_LeavesUnknownUnchanged()
    {
        var service = new EmailTemplateService(null!);

        var result = service.MergeTokens(
            "Hello {EmployeeName}, your payslip for {Period} is ready. Ref: {UnknownToken}",
            new Dictionary<string, string>
            {
                ["EmployeeName"] = "Ada Okafor",
                ["Period"] = "July 2026",
            });

        result.Should().Be(
            "Hello Ada Okafor, your payslip for July 2026 is ready. Ref: {UnknownToken}");
    }

    [Fact]
    public void MergeTokens_EmptyTemplate_ReturnsEmpty()
    {
        var service = new EmailTemplateService(null!);

        service.MergeTokens(string.Empty, new Dictionary<string, string>())
            .Should().BeEmpty();
    }

    [Fact]
    public void GetBuiltInDefaults_LeaveSubmitted_ContainsEmployeeNameToken()
    {
        var defaults = EmailTemplateService.GetBuiltInDefaults("EML-001");

        defaults.Subject.Should().Contain("{EmployeeName}");
        defaults.BodyHtml.Should().Contain("{Reason}");
    }
}
