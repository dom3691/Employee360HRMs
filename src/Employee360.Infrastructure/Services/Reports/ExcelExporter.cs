using ClosedXML.Excel;
using Employee360.Application.Common.Interfaces;

namespace Employee360.Infrastructure.Services.Reports;

/// <summary>Excel (.xlsx) exporter using ClosedXML (FR-RPT export).</summary>
public sealed class ExcelExporter : IExcelExporter
{
    public byte[] Export(
        string sheetName,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<object?>> rows)
    {
        using var workbook = new XLWorkbook();
        var safeName = sheetName.Length > 31 ? sheetName[..31] : sheetName;
        var worksheet = workbook.Worksheets.Add(string.IsNullOrWhiteSpace(safeName) ? "Report" : safeName);

        for (var c = 0; c < headers.Count; c++)
        {
            worksheet.Cell(1, c + 1).Value = headers[c];
            worksheet.Cell(1, c + 1).Style.Font.Bold = true;
        }

        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            for (var c = 0; c < row.Count; c++)
            {
                SetCellValue(worksheet.Cell(r + 2, c + 1), row[c]);
            }
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void SetCellValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                cell.Value = Blank.Value;
                break;
            case decimal m:
                cell.Value = m;
                cell.Style.NumberFormat.Format = "#,##0.00";
                break;
            case double d:
                cell.Value = d;
                break;
            case int i:
                cell.Value = i;
                break;
            case DateOnly date:
                cell.Value = date.ToDateTime(TimeOnly.MinValue);
                cell.Style.DateFormat.Format = "yyyy-mm-dd";
                break;
            case DateTime dt:
                cell.Value = dt;
                cell.Style.DateFormat.Format = "yyyy-mm-dd";
                break;
            default:
                cell.Value = value.ToString();
                break;
        }
    }
}
