using Employee360.Application.Common.Services;
using FluentAssertions;

namespace Employee360.UnitTests.Application.Payroll;

public class BankPaymentFileExporterTests
{
    [Fact]
    public void NibssNeftTemplate_ShouldEmitExpectedHeaderAndColumns()
    {
        var exporter = new BankPaymentFileExporter();

        var bytes = exporter.Export(new BankPaymentFileRequest(
            "August 2026",
            "Salary August 2026",
            [
                new BankPaymentLine("0123456789", "Ada Okafor", "058", 210350.00m, "August 2026-EMP-001"),
            ],
            "NibssNeft"));

        var csv = System.Text.Encoding.UTF8.GetString(bytes);

        csv.Should().Contain("BeneficiaryAccount,BeneficiaryName,BankCode,Amount,Narration,PaymentReference");
        csv.Should().Contain("0123456789");
        csv.Should().Contain("Ada Okafor");
        csv.Should().Contain("058");
        csv.Should().Contain("210350.00");
        csv.Should().Contain("Salary August 2026");
    }

    [Fact]
    public void SimpleCsvTemplate_ShouldUseAlternateHeader()
    {
        var exporter = new BankPaymentFileExporter();

        var bytes = exporter.Export(new BankPaymentFileRequest(
            "August 2026",
            "Salary August 2026",
            [new BankPaymentLine("0123456789", "Ada Okafor", "058", 100m, "REF")],
            "SimpleCsv"));

        var csv = System.Text.Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF');
        csv.Should().StartWith("AccountNumber,AccountName,BankCode,Amount,Narration,Reference");
    }
}
