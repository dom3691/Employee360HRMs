namespace Employee360.Application.Common.Models;

/// <summary>Payroll run and bank file settings (FR-PAY-010).</summary>
public sealed class PayrollSettings
{
    public const string SectionName = "Payroll";

    /// <summary>Bank file template: NibssNeft or SimpleCsv.</summary>
    public string BankFileTemplate { get; init; } = "NibssNeft";

    /// <summary>Narration format; {Period} is replaced with period label.</summary>
    public string BankFileNarrationFormat { get; init; } = "Salary {Period}";

    /// <summary>Default expected working days per month for pay-factor calculation.</summary>
    public int DefaultWorkingDaysPerMonth { get; init; } = 22;
}
