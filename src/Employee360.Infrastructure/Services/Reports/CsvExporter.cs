using System.Globalization;
using System.Text;
using Employee360.Application.Common.Interfaces;

namespace Employee360.Infrastructure.Services.Reports;

/// <summary>UTF-8 CSV exporter with BOM for Excel compatibility.</summary>
public sealed class CsvExporter : ICsvExporter
{
    public byte[] Export(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<object?>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', headers.Select(Escape)));

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(',', row.Select(v => Escape(Format(v)))));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    internal static string Format(object? value) =>
        value switch
        {
            null => string.Empty,
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
            decimal m => m.ToString("F2", CultureInfo.InvariantCulture),
            double d => d.ToString("F2", CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };

    internal static string Escape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }
}
