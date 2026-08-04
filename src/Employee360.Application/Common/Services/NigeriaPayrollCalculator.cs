using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Constants;
using Employee360.Domain.Enums;

namespace Employee360.Application.Common.Services;

/// <summary>
/// Pure, config-driven Nigeria payroll calculator (FR-PAY-003..007, Appendix B/C).
/// All statutory rates and tax bands are supplied via <see cref="PayrollCalculationRequest"/>.
/// </summary>
public sealed class NigeriaPayrollCalculator : INigeriaPayrollCalculator
{
    /// <inheritdoc />
    public PayrollCalculationResult Calculate(PayrollCalculationRequest request)
    {
        var payFactor = Math.Clamp(request.PayFactor, 0m, 1m);

        var basic = Round(request.Basic * payFactor);
        var housing = Round(request.Housing * payFactor);
        var transport = Round(request.Transport * payFactor);
        var other = Round(request.OtherAllowances * payFactor);
        var gross = Round(basic + housing + transport + other);

        var pensionable = Round(basic + housing + transport);
        var annualGross = gross * 12m;

        var rates = request.StatutoryRates.ToDictionary(r => r.Code, StringComparer.Ordinal);

        var craAnnual = CalculateCraAnnual(annualGross, rates);
        var craMonthly = Round(craAnnual / 12m);

        var taxableAnnual = Math.Max(0m, annualGross - craAnnual);
        var payeAnnual = CalculateProgressiveTax(
            taxableAnnual,
            request.TaxBands.OrderBy(b => b.BandOrder).ToList());
        var paye = Round(payeAnnual / 12m);

        var pensionEmployee = Round(CalculateRateAmount(
            rates, PayrollStatutoryRateCodes.PensionEmployee, pensionable, isEmployee: true));
        var pensionEmployer = Round(CalculateRateAmount(
            rates, PayrollStatutoryRateCodes.PensionEmployer, pensionable, isEmployee: false));

        var nhf = Round(CalculateRateAmount(
            rates, PayrollStatutoryRateCodes.NhfEmployee, basic, isEmployee: true));

        var nsitfEmployer = Round(CalculateRateAmount(
            rates, PayrollStatutoryRateCodes.NsitfEmployer, gross, isEmployee: false));

        var customDeductions = request.CustomDeductions
            .Select(d => new PayrollCustomDeductionInput(d.Name, Round(d.Amount * payFactor)))
            .ToList();

        var customTotal = Round(customDeductions.Sum(d => d.Amount));

        var totalEmployeeDeductions = Round(paye + pensionEmployee + nhf + customTotal);
        var netPay = Round(gross - totalEmployeeDeductions);

        return new PayrollCalculationResult
        {
            Basic = basic,
            Housing = housing,
            Transport = transport,
            OtherAllowances = other,
            GrossPay = gross,
            PayFactor = payFactor,
            CraAnnual = Round(craAnnual),
            CraMonthly = craMonthly,
            TaxableIncomeAnnual = Round(taxableAnnual),
            TaxableIncomeMonthly = Round(taxableAnnual / 12m),
            PayeAnnual = Round(payeAnnual),
            Paye = paye,
            PensionEmployee = pensionEmployee,
            PensionEmployer = pensionEmployer,
            Nhf = nhf,
            NsitfEmployer = nsitfEmployer,
            CustomDeductionsTotal = customTotal,
            CustomDeductions = customDeductions,
            TotalEmployeeDeductions = totalEmployeeDeductions,
            NetPay = netPay,
        };
    }

    /// <summary>
    /// CRA: higher of fixed minimum or 1% of gross, PLUS additional % of gross (annual).
    /// </summary>
    public static decimal CalculateCraAnnual(
        decimal annualGross,
        IReadOnlyDictionary<string, PayrollStatutoryRateInput> rates)
    {
        var fixedMinimum = GetFixedAnnual(rates, PayrollStatutoryRateCodes.CraFixedMinimum);
        var grossPercent = GetVariableRate(rates, PayrollStatutoryRateCodes.CraGrossPercent);
        var additionalPercent = GetVariableRate(rates, PayrollStatutoryRateCodes.CraAdditionalGrossPercent);

        var partOne = Math.Max(fixedMinimum, annualGross * grossPercent);
        return partOne + annualGross * additionalPercent;
    }

    /// <summary>Progressive PAYE on annual taxable income using cumulative upper bounds.</summary>
    public static decimal CalculateProgressiveTax(
        decimal taxableAnnual,
        IReadOnlyList<PayrollTaxBandInput> bands)
    {
        if (taxableAnnual <= 0 || bands.Count == 0)
        {
            return 0m;
        }

        decimal tax = 0m;
        decimal previousUpper = 0m;

        foreach (var band in bands)
        {
            if (taxableAnnual <= previousUpper)
            {
                break;
            }

            var upper = band.UpperBoundAnnual ?? decimal.MaxValue;
            var taxableInBand = Math.Min(taxableAnnual, upper) - previousUpper;

            if (taxableInBand > 0)
            {
                tax += taxableInBand * band.Rate;
            }

            previousUpper = upper;
        }

        return tax;
    }

    private static decimal CalculateRateAmount(
        IReadOnlyDictionary<string, PayrollStatutoryRateInput> rates,
        string code,
        decimal basisAmount,
        bool isEmployee)
    {
        if (!rates.TryGetValue(code, out var rate))
        {
            return 0m;
        }

        var fraction = isEmployee ? rate.EmployeeRate : rate.EmployerRate;

        return fraction.HasValue ? basisAmount * fraction.Value : 0m;
    }

    private static decimal GetFixedAnnual(
        IReadOnlyDictionary<string, PayrollStatutoryRateInput> rates,
        string code)
        => rates.TryGetValue(code, out var rate) ? rate.FixedAnnualAmount ?? 0m : 0m;

    private static decimal GetVariableRate(
        IReadOnlyDictionary<string, PayrollStatutoryRateInput> rates,
        string code)
        => rates.TryGetValue(code, out var rate) ? rate.VariableRate ?? 0m : 0m;

    /// <summary>Rounds to kobo (2 decimal places).</summary>
    public static decimal Round(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
