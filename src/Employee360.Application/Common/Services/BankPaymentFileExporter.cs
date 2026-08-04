using System.Globalization;
using System.Text;
using Employee360.Application.Common.Models;

namespace Employee360.Application.Common.Services;

/// <summary>
/// Exports NIBSS/NEFT-style bank payment CSV (FR-PAY-010).
/// </summary>
public interface IBankPaymentFileExporter
{
    byte[] Export(BankPaymentFileRequest request);
}

public sealed record BankPaymentFileRequest(
    string PeriodLabel,
    string Narration,
    IReadOnlyList<BankPaymentLine> Lines,
    string Template);

public sealed record BankPaymentLine(
    string AccountNumber,
    string AccountName,
    string? BankCode,
    decimal Amount,
    string Reference);

public sealed class BankPaymentFileExporter : IBankPaymentFileExporter
{
    public byte[] Export(BankPaymentFileRequest request)
    {
        var sb = new StringBuilder();

        if (request.Template.Equals("SimpleCsv", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("AccountNumber,AccountName,BankCode,Amount,Narration,Reference");
            foreach (var line in request.Lines)
            {
                sb.AppendLine(string.Join(',',
                    Csv(line.AccountNumber),
                    Csv(line.AccountName),
                    Csv(line.BankCode ?? string.Empty),
                    line.Amount.ToString("F2", CultureInfo.InvariantCulture),
                    Csv(request.Narration),
                    Csv(line.Reference)));
            }
        }
        else
        {
            // NIBSS/NEFT default template (FR-PAY-010).
            sb.AppendLine("BeneficiaryAccount,BeneficiaryName,BankCode,Amount,Narration,PaymentReference");
            foreach (var line in request.Lines)
            {
                sb.AppendLine(string.Join(',',
                    Csv(line.AccountNumber),
                    Csv(line.AccountName),
                    Csv(line.BankCode ?? "000"),
                    line.Amount.ToString("F2", CultureInfo.InvariantCulture),
                    Csv(request.Narration),
                    Csv(line.Reference)));
            }
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string Csv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }
}
