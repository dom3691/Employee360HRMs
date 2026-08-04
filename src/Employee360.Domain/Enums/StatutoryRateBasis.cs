namespace Employee360.Domain.Enums;

/// <summary>Basis amount for a statutory payroll rate (FR-PAY-003..006).</summary>
public enum StatutoryRateBasis
{
    /// <summary>Total monthly emoluments (basic + housing + transport + other).</summary>
    Gross = 0,

    /// <summary>Basic salary only.</summary>
    Basic = 1,

    /// <summary>Basic + housing + transport (pensionable emoluments).</summary>
    PensionableEmoluments = 2,
}
