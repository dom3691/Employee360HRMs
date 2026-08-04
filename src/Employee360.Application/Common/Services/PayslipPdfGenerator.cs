using System.Globalization;
using System.Text;
using Employee360.Application.Common.Interfaces;

namespace Employee360.Application.Common.Services;

/// <summary>
/// Generates itemized payslip PDF bytes (FR-PAY-009) without external dependencies.
/// </summary>
public interface IPayslipPdfGenerator
{
    byte[] Generate(PayslipPdfModel model);
}

public sealed record PayslipPdfModel(
    string CompanyName,
    string PeriodLabel,
    string EmployeeCode,
    string EmployeeName,
    decimal Basic,
    decimal Housing,
    decimal Transport,
    decimal OtherAllowances,
    decimal GrossPay,
    decimal PayFactor,
    decimal CraMonthly,
    decimal Paye,
    decimal PensionEmployee,
    decimal PensionEmployer,
    decimal Nhf,
    decimal NsitfEmployer,
    decimal CustomDeductionsTotal,
    decimal NetPay,
    IReadOnlyList<PayrollCustomDeductionInput> CustomDeductions,
    DateTime GeneratedAtWat);

public sealed class PayslipPdfGenerator : IPayslipPdfGenerator
{
    public byte[] Generate(PayslipPdfModel model)
    {
        var lines = BuildLines(model);
        return SimplePdfWriter.Write(lines);
    }

    private static IReadOnlyList<string> BuildLines(PayslipPdfModel m)
    {
        var list = new List<string>
        {
            m.CompanyName,
            $"Payslip — {m.PeriodLabel}",
            $"Employee: {m.EmployeeName} ({m.EmployeeCode})",
            $"Generated (WAT): {m.GeneratedAtWat:yyyy-MM-dd HH:mm}",
            "",
            "Earnings",
            $"  Basic:           {FormatNaira(m.Basic)}",
            $"  Housing:         {FormatNaira(m.Housing)}",
            $"  Transport:       {FormatNaira(m.Transport)}",
            $"  Other:           {FormatNaira(m.OtherAllowances)}",
            $"  Gross Pay:       {FormatNaira(m.GrossPay)}",
            $"  Pay Factor:      {m.PayFactor:P2}",
            "",
            "Statutory Deductions",
            $"  CRA (monthly):   {FormatNaira(m.CraMonthly)}",
            $"  PAYE:            {FormatNaira(m.Paye)}",
            $"  Pension (EE):    {FormatNaira(m.PensionEmployee)}",
            $"  Pension (ER):    {FormatNaira(m.PensionEmployer)}",
            $"  NHF:             {FormatNaira(m.Nhf)}",
            $"  NSITF (ER):      {FormatNaira(m.NsitfEmployer)}",
        };

        if (m.CustomDeductions.Count > 0)
        {
            list.Add("");
            list.Add("Other Deductions");
            foreach (var d in m.CustomDeductions)
            {
                list.Add($"  {d.Name}: {FormatNaira(d.Amount)}");
            }

            list.Add($"  Total:           {FormatNaira(m.CustomDeductionsTotal)}");
        }

        list.Add("");
        list.Add($"NET PAY:           {FormatNaira(m.NetPay)}");

        return list;
    }

    private static string FormatNaira(decimal amount) =>
        amount.ToString("C2", CultureInfo.GetCultureInfo("en-NG"));
}

/// <summary>Minimal PDF 1.4 writer for plain-text payslips.</summary>
internal static class SimplePdfWriter
{
    public static byte[] Write(IReadOnlyList<string> lines)
    {
        var content = BuildContentStream(lines);
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        };

        var sb = new StringBuilder();
        sb.AppendLine("%PDF-1.4");

        var offsets = new List<int>();

        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString()));
            sb.AppendLine($"{i + 1} 0 obj");
            sb.AppendLine(objects[i]);
            sb.AppendLine("endobj");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(sb.ToString());
        sb.AppendLine("xref");
        sb.AppendLine($"0 {objects.Count + 1}");
        sb.AppendLine("0000000000 65535 f ");

        foreach (var offset in offsets)
        {
            sb.AppendLine($"{offset:D10} 00000 n ");
        }

        sb.AppendLine("trailer");
        sb.AppendLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        sb.AppendLine("startxref");
        sb.AppendLine(xrefOffset.ToString());
        sb.AppendLine("%%EOF");

        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private static string BuildContentStream(IReadOnlyList<string> lines)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BT");
        sb.AppendLine("/F1 11 Tf");
        sb.AppendLine("50 750 Td");
        sb.AppendLine("14 TL");

        foreach (var line in lines)
        {
            sb.AppendLine($"({Escape(line)}) Tj");
            sb.AppendLine("T*");
        }

        sb.AppendLine("ET");
        return sb.ToString();
    }

    private static string Escape(string text) =>
        text.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
}
