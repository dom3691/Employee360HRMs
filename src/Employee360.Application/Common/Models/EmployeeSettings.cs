namespace Employee360.Application.Common.Models;

/// <summary>
/// Employee module settings ("Employee" configuration section).
/// Controls the FR-EMP-001 employee code format, e.g. EMP-00001.
/// </summary>
public sealed class EmployeeSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Employee";

    /// <summary>Code prefix (default "EMP").</summary>
    public string CodePrefix { get; init; } = "EMP";

    /// <summary>Zero-padded digit count for the sequence (default 5 → 00001).</summary>
    public int CodeDigits { get; init; } = 5;

    /// <summary>Formats a sequence number into an employee code.</summary>
    /// <param name="sequence">1-based sequence number.</param>
    public string FormatCode(int sequence) =>
        $"{CodePrefix}-{sequence.ToString().PadLeft(CodeDigits, '0')}";
}
