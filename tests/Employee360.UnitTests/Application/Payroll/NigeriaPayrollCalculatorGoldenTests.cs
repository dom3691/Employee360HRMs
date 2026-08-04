using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Services;
using Employee360.Domain.Constants;
using Employee360.Domain.Enums;
using Employee360.Infrastructure.Persistence.Seeding;
using FluentAssertions;

namespace Employee360.UnitTests.Application.Payroll;

/// <summary>
/// Golden-master tests for Nigeria payroll calculation (FR-PAY-003..007).
/// Expected values computed from PRD Appendix B/C 2024 reference config.
/// </summary>
public class NigeriaPayrollCalculatorGoldenTests
{
    private readonly NigeriaPayrollCalculator _calculator = new();

    private static PayrollCalculationRequest BuildRequest(
        decimal basic,
        decimal housing,
        decimal transport,
        decimal other,
        decimal payFactor = 1.0m,
        IReadOnlyList<PayrollCustomDeductionInput>? custom = null)
        => new()
        {
            Basic = basic,
            Housing = housing,
            Transport = transport,
            OtherAllowances = other,
            PayFactor = payFactor,
            TaxBands = ReferenceTaxBands(),
            StatutoryRates = ReferenceStatutoryRates(),
            CustomDeductions = custom ?? Array.Empty<PayrollCustomDeductionInput>(),
        };

    public static IReadOnlyList<PayrollTaxBandInput> ReferenceTaxBands() =>
    [
        new(1, 800_000m, 0m),
        new(2, 3_000_000m, 0.15m),
        new(3, 12_000_000m, 0.18m),
        new(4, 25_000_000m, 0.21m),
        new(5, 50_000_000m, 0.23m),
        new(6, null, 0.25m),
    ];

    public static IReadOnlyList<PayrollStatutoryRateInput> ReferenceStatutoryRates() =>
    [
        new(PayrollStatutoryRateCodes.CraFixedMinimum, null, null, 200_000m, null, StatutoryRateBasis.Gross),
        new(PayrollStatutoryRateCodes.CraGrossPercent, null, null, null, 0.01m, StatutoryRateBasis.Gross),
        new(PayrollStatutoryRateCodes.CraAdditionalGrossPercent, null, null, null, 0.20m, StatutoryRateBasis.Gross),
        new(PayrollStatutoryRateCodes.PensionEmployee, 0.08m, null, null, null, StatutoryRateBasis.PensionableEmoluments),
        new(PayrollStatutoryRateCodes.PensionEmployer, null, 0.10m, null, null, StatutoryRateBasis.PensionableEmoluments),
        new(PayrollStatutoryRateCodes.NhfEmployee, 0.025m, null, null, null, StatutoryRateBasis.Basic),
        new(PayrollStatutoryRateCodes.NsitfEmployer, null, 0.01m, null, null, StatutoryRateBasis.Gross),
    ];

    [Fact]
    public void LowEarner_250kGross_CalculatesExpectedStatutoryDeductions()
    {
        var result = _calculator.Calculate(BuildRequest(150_000m, 50_000m, 30_000m, 20_000m));

        result.GrossPay.Should().Be(250_000m);
        result.CraAnnual.Should().Be(800_000m);
        result.TaxableIncomeAnnual.Should().Be(2_200_000m);
        result.Paye.Should().Be(17_500m);
        result.PensionEmployee.Should().Be(18_400m);
        result.PensionEmployer.Should().Be(23_000m);
        result.Nhf.Should().Be(3_750m);
        result.NsitfEmployer.Should().Be(2_500m);
        result.NetPay.Should().Be(210_350m);
    }

    [Fact]
    public void MidEarner_850kGross_CalculatesExpectedPayeAndNet()
    {
        var result = _calculator.Calculate(BuildRequest(500_000m, 200_000m, 100_000m, 50_000m));

        result.GrossPay.Should().Be(850_000m);
        result.CraAnnual.Should().Be(2_240_000m);
        result.TaxableIncomeAnnual.Should().Be(7_960_000m);
        result.PayeAnnual.Should().Be(1_222_800m);
        result.Paye.Should().Be(101_900m);
        result.PensionEmployee.Should().Be(64_000m);
        result.Nhf.Should().Be(12_500m);
        result.NetPay.Should().Be(671_600m);
    }

    [Fact]
    public void HighEarner_4mGross_HitsUpperTaxBands()
    {
        var result = _calculator.Calculate(BuildRequest(2_000_000m, 1_000_000m, 500_000m, 500_000m));

        result.GrossPay.Should().Be(4_000_000m);
        result.CraAnnual.Should().Be(10_080_000m);
        result.TaxableIncomeAnnual.Should().Be(37_920_000m);
        result.PayeAnnual.Should().Be(7_651_600m);
        result.Paye.Should().Be(637_633.33m);
        result.PensionEmployee.Should().Be(280_000m);
        result.Nhf.Should().Be(50_000m);
        result.NetPay.Should().Be(3_032_366.67m);
    }

    [Fact]
    public void CustomDeductions_AreSubtractedFromNet()
    {
        var result = _calculator.Calculate(BuildRequest(
            500_000m, 200_000m, 100_000m, 50_000m,
            custom: [new PayrollCustomDeductionInput("Cooperative", 10_000m)]));

        result.CustomDeductionsTotal.Should().Be(10_000m);
        result.NetPay.Should().Be(661_600m);
    }

    [Fact]
    public void PayFactor_ProratesGrossAndDeductions_FR_PAY_012()
    {
        const decimal payFactor = 20m / 22m;

        var result = _calculator.Calculate(BuildRequest(
            500_000m, 200_000m, 100_000m, 50_000m,
            payFactor: payFactor,
            custom: [new PayrollCustomDeductionInput("Loan", 5_000m)]));

        result.GrossPay.Should().Be(NigeriaPayrollCalculator.Round(850_000m * payFactor));
        result.PayFactor.Should().Be(payFactor);
        result.NetPay.Should().BeLessThan(671_600m);
    }

    [Fact]
    public void CalculateCraAnnual_UsesHigherOfFixedOrOnePercent()
    {
        var rates = ReferenceStatutoryRates().ToDictionary(r => r.Code, StringComparer.Ordinal);

        NigeriaPayrollCalculator.CalculateCraAnnual(3_000_000m, rates)
            .Should().Be(800_000m);

        NigeriaPayrollCalculator.CalculateCraAnnual(48_000_000m, rates)
            .Should().Be(10_080_000m);
    }

    [Fact]
    public void SeededTaxYear_MatchesReferenceBandCount()
    {
        PayrollConfigSeeder.DefaultTaxYear.Should().Be(2024);
        ReferenceTaxBands().Should().HaveCount(6);
        ReferenceStatutoryRates().Should().HaveCount(7);
    }
}
