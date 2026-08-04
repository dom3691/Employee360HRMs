namespace Employee360.Application.Common.Interfaces;

/// <summary>
/// Generic CSV exporter reused across reports and admin exports (FR-RPT).
/// </summary>
public interface ICsvExporter
{
    byte[] Export(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<object?>> rows);
}

/// <summary>
/// Generic Excel exporter reused across reports (FR-RPT).
/// </summary>
public interface IExcelExporter
{
    byte[] Export(string sheetName, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<object?>> rows);
}
