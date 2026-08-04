namespace Employee360.Application.Common.Interfaces;

/// <summary>
/// Input to the Nigeria payroll calculation engine (FR-PAY-001..007, FR-PAY-012).
/// All rates and bands are supplied from config — nothing is hard-coded.
/// </summary>
public sealed record PayrollCalculationRequest
{
    /// <summary>Monthly basic before pro-rating.</summary>
    public required decimal Basic { get; init; }

    /// <summary>Monthly housing before pro-rating.</summary>
    public required decimal Housing { get; init; }

    /// <summary>Monthly transport before pro-rating.</summary>
    public required decimal Transport { get; init; }

    /// <summary>Other monthly allowances before pro-rating.</summary>
    public required decimal OtherAllowances { get; init; }

    /// <summary>
    /// Pro-rating factor for unpaid leave / attendance (FR-PAY-012).
    /// 1.0 = full month; 0.90909 ≈ 20/22 working days.
    /// </summary>
    public decimal PayFactor { get; init; } = 1.0m;

    /// <summary>Active PAYE bands for the tax year, ordered by BandOrder.</summary>
    public required IReadOnlyList<PayrollTaxBandInput> TaxBands { get; init; }

    /// <summary>Statutory rate configuration rows for the tax year.</summary>
    public required IReadOnlyList<PayrollStatutoryRateInput> StatutoryRates { get; init; }

    /// <summary>Custom employee deductions to apply this period.</summary>
    public IReadOnlyList<PayrollCustomDeductionInput> CustomDeductions { get; init; } =
        Array.Empty<PayrollCustomDeductionInput>();
}

/// <summary>PAYE band input row.</summary>
public sealed record PayrollTaxBandInput(
    int BandOrder,
    decimal? UpperBoundAnnual,
    decimal Rate);

/// <summary>Statutory rate config input row.</summary>
public sealed record PayrollStatutoryRateInput(
    string Code,
    decimal? EmployeeRate,
    decimal? EmployerRate,
    decimal? FixedAnnualAmount,
    decimal? VariableRate,
    Domain.Enums.StatutoryRateBasis Basis);

/// <summary>Custom deduction input row.</summary>
public sealed record PayrollCustomDeductionInput(
    string Name,
    decimal Amount);

/// <summary>
/// Full Nigeria payroll calculation result — all monetary values in ₦ to 2 dp (kobo).
/// </summary>
public sealed record PayrollCalculationResult
{
    public decimal Basic { get; init; }
    public decimal Housing { get; init; }
    public decimal Transport { get; init; }
    public decimal OtherAllowances { get; init; }
    public decimal GrossPay { get; init; }
    public decimal PayFactor { get; init; }
    public decimal CraAnnual { get; init; }
    public decimal CraMonthly { get; init; }
    public decimal TaxableIncomeAnnual { get; init; }
    public decimal TaxableIncomeMonthly { get; init; }
    public decimal PayeAnnual { get; init; }
    public decimal Paye { get; init; }
    public decimal PensionEmployee { get; init; }
    public decimal PensionEmployer { get; init; }
    public decimal Nhf { get; init; }
    public decimal NsitfEmployer { get; init; }
    public decimal CustomDeductionsTotal { get; init; }
    public IReadOnlyList<PayrollCustomDeductionInput> CustomDeductions { get; init; } =
        Array.Empty<PayrollCustomDeductionInput>();
    public decimal TotalEmployeeDeductions { get; init; }
    public decimal NetPay { get; init; }
}

/// <summary>
/// Nigeria-compliant payroll calculation engine (FR-PAY-003..007, FR-PAY-012).
/// </summary>
public interface INigeriaPayrollCalculator
{
    /// <summary>Calculates gross-to-net pay using supplied config tables.</summary>
    PayrollCalculationResult Calculate(PayrollCalculationRequest request);
}
