using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Constants;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Employee360.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds Nigeria PAYE tax bands and statutory rates for the reference tax year
/// (PRD Appendix B/C — configurable, not hard-coded in calculator).
/// </summary>
public sealed class PayrollConfigSeeder : IDataSeeder
{
    public const int DefaultTaxYear = 2024;

    private readonly Employee360DbContext _context;
    private readonly ILogger<PayrollConfigSeeder> _logger;

    public PayrollConfigSeeder(Employee360DbContext context, ILogger<PayrollConfigSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedTaxBandsAsync(cancellationToken);
        await SeedStatutoryRatesAsync(cancellationToken);
        _logger.LogInformation("Payroll config seed data applied for tax year {TaxYear}", DefaultTaxYear);
    }

    private async Task SeedTaxBandsAsync(CancellationToken cancellationToken)
    {
        if (await _context.TaxBands.AnyAsync(b => b.TaxYear == DefaultTaxYear, cancellationToken))
        {
            return;
        }

        var bands = new[]
        {
            new TaxBand { TaxYear = DefaultTaxYear, BandOrder = 1, UpperBoundAnnual = 800_000m, Rate = 0m },
            new TaxBand { TaxYear = DefaultTaxYear, BandOrder = 2, UpperBoundAnnual = 3_000_000m, Rate = 0.15m },
            new TaxBand { TaxYear = DefaultTaxYear, BandOrder = 3, UpperBoundAnnual = 12_000_000m, Rate = 0.18m },
            new TaxBand { TaxYear = DefaultTaxYear, BandOrder = 4, UpperBoundAnnual = 25_000_000m, Rate = 0.21m },
            new TaxBand { TaxYear = DefaultTaxYear, BandOrder = 5, UpperBoundAnnual = 50_000_000m, Rate = 0.23m },
            new TaxBand { TaxYear = DefaultTaxYear, BandOrder = 6, UpperBoundAnnual = null, Rate = 0.25m },
        };

        await _context.TaxBands.AddRangeAsync(bands, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedStatutoryRatesAsync(CancellationToken cancellationToken)
    {
        if (await _context.PayrollStatutoryRates.AnyAsync(r => r.TaxYear == DefaultTaxYear, cancellationToken))
        {
            return;
        }

        var rates = new[]
        {
            new PayrollStatutoryRate
            {
                Code = PayrollStatutoryRateCodes.CraFixedMinimum,
                Name = "CRA fixed minimum (annual)",
                TaxYear = DefaultTaxYear,
                FixedAnnualAmount = 200_000m,
                Basis = StatutoryRateBasis.Gross,
            },
            new PayrollStatutoryRate
            {
                Code = PayrollStatutoryRateCodes.CraGrossPercent,
                Name = "CRA 1% of gross (annual)",
                TaxYear = DefaultTaxYear,
                VariableRate = 0.01m,
                Basis = StatutoryRateBasis.Gross,
            },
            new PayrollStatutoryRate
            {
                Code = PayrollStatutoryRateCodes.CraAdditionalGrossPercent,
                Name = "CRA additional 20% of gross (annual)",
                TaxYear = DefaultTaxYear,
                VariableRate = 0.20m,
                Basis = StatutoryRateBasis.Gross,
            },
            new PayrollStatutoryRate
            {
                Code = PayrollStatutoryRateCodes.PensionEmployee,
                Name = "Pension employee contribution",
                TaxYear = DefaultTaxYear,
                EmployeeRate = 0.08m,
                Basis = StatutoryRateBasis.PensionableEmoluments,
            },
            new PayrollStatutoryRate
            {
                Code = PayrollStatutoryRateCodes.PensionEmployer,
                Name = "Pension employer contribution",
                TaxYear = DefaultTaxYear,
                EmployerRate = 0.10m,
                Basis = StatutoryRateBasis.PensionableEmoluments,
            },
            new PayrollStatutoryRate
            {
                Code = PayrollStatutoryRateCodes.NhfEmployee,
                Name = "NHF employee contribution",
                TaxYear = DefaultTaxYear,
                EmployeeRate = 0.025m,
                Basis = StatutoryRateBasis.Basic,
            },
            new PayrollStatutoryRate
            {
                Code = PayrollStatutoryRateCodes.NsitfEmployer,
                Name = "NSITF employer contribution",
                TaxYear = DefaultTaxYear,
                EmployerRate = 0.01m,
                Basis = StatutoryRateBasis.Gross,
            },
        };

        await _context.PayrollStatutoryRates.AddRangeAsync(rates, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
