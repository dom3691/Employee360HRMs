namespace Employee360.Domain.Enums;

/// <summary>Origin of an attendance clock event (FR-ATT-001, FR-ATT-005).</summary>
public enum AttendanceSource
{
    /// <summary>Web self-service clock-in/out.</summary>
    Web = 0,

    /// <summary>Biometric or third-party device ingestion.</summary>
    Device = 1,

    /// <summary>HR/Manager manual correction (FR-ATT-006).</summary>
    Manual = 2,
}
