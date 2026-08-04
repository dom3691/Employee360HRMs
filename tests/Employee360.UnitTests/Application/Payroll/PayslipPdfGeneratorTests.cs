using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Services;
using FluentAssertions;

namespace Employee360.UnitTests.Application.Payroll;

public class PayslipPdfGeneratorTests
{
    [Fact]
    public void Generate_ShouldProduceValidPdfHeader()
    {
        var generator = new PayslipPdfGenerator();

        var pdf = generator.Generate(new PayslipPdfModel(
            "Acme Nigeria Ltd",
            "August 2026",
            "EMP-001",
            "Ada Okafor",
            150_000m, 50_000m, 30_000m, 20_000m,
            250_000m, 1.0m, 66_666.67m,
            17_500m, 18_400m, 23_000m, 3_750m, 2_500m,
            0m, 210_350m,
            Array.Empty<PayrollCustomDeductionInput>(),
            new DateTime(2026, 8, 28, 11, 0, 0)));

        var header = System.Text.Encoding.ASCII.GetString(pdf.AsSpan(0, 8));
        header.Should().Be("%PDF-1.4");
    }
}
