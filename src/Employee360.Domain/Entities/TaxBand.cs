using Employee360.Domain.Common;
using Employee360.Domain.Enums;

namespace Employee360.Domain.Entities;

/// <summary>
/// Configurable PAYE tax band for a fiscal/tax year (FR-PAY-003, Appendix B).
/// Bands use cumulative annual upper bounds for progressive calculation.
/// </summary>
public class TaxBand : AuditableEntity
{
    /// <summary>Tax year the band applies to, e.g. 2024.</summary>
    public int TaxYear { get; set; }

    /// <summary>Evaluation order (1 = lowest band).</summary>
    public int BandOrder { get; set; }

    /// <summary>Cumulative annual taxable income upper bound (₦); null = unlimited top band.</summary>
    public decimal? UpperBoundAnnual { get; set; }

    /// <summary>Marginal rate as a decimal fraction, e.g. 0.15 for 15%.</summary>
    public decimal Rate { get; set; }

    /// <summary>When false, excluded from calculation.</summary>
    public bool IsActive { get; set; } = true;
}
