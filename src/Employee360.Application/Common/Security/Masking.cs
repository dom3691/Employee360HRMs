namespace Employee360.Application.Common.Security;

/// <summary>
/// Masks sensitive values for display (NFR-SEC-004: bank account and NIN are
/// masked in the UI, showing only the trailing digits).
/// </summary>
public static class Masking
{
    /// <summary>
    /// Replaces all but the last <paramref name="visibleSuffixLength"/> characters
    /// with asterisks. Values shorter than or equal to the visible length are fully
    /// masked to avoid leaking short secrets.
    /// </summary>
    /// <param name="value">The sensitive value.</param>
    /// <param name="visibleSuffixLength">Trailing characters to reveal (default 4).</param>
    public static string Mask(string? value, int visibleSuffixLength = 4)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Length <= visibleSuffixLength)
        {
            return new string('*', value.Length);
        }

        return string.Concat(
            new string('*', value.Length - visibleSuffixLength),
            value.AsSpan(value.Length - visibleSuffixLength));
    }
}
