namespace Employee360.Domain.Enums;

/// <summary>Employee document categories (PRD FR-EMP-007).</summary>
public enum DocumentCategory
{
    /// <summary>Employment contract.</summary>
    Contract = 0,

    /// <summary>Government ID (national ID card, passport, driver's licence).</summary>
    Identification = 1,

    /// <summary>Offer letter.</summary>
    OfferLetter = 2,

    /// <summary>Academic or professional certificate.</summary>
    Certificate = 3,

    /// <summary>Work permit / visa (supports expiry tracking).</summary>
    WorkPermit = 4,

    /// <summary>Anything else.</summary>
    Other = 5,
}
