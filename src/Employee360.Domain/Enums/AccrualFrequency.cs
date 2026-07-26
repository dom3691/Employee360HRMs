namespace Employee360.Domain.Enums;

/// <summary>How leave entitlement accrues (PRD FR-LV-002).</summary>
public enum AccrualFrequency
{
    /// <summary>Full annual entitlement granted at the start of the year.</summary>
    Annual = 0,

    /// <summary>Entitlement accrues in twelve monthly portions.</summary>
    Monthly = 1,
}
