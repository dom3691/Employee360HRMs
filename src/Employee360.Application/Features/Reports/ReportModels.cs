namespace Employee360.Application.Features.Reports;

/// <summary>Tabular report payload used for JSON display and CSV/Excel export.</summary>
public sealed class ReportTable
{
    public required string Title { get; init; }

    public required IReadOnlyList<string> Headers { get; init; }

    public required IReadOnlyList<IReadOnlyList<object?>> Rows { get; init; }

    /// <summary>Optional structured summary returned in JSON responses.</summary>
    public object? Summary { get; init; }
}

/// <summary>API response for GET /api/v1/reports/{reportId}.</summary>
public sealed record ReportResponse(
    string ReportId,
    string Title,
    DateOnly? FromDate,
    DateOnly? ToDate,
    object? Data,
    byte[]? ExportBytes,
    string? ContentType,
    string? FileName);

/// <summary>Normalized date range for report queries.</summary>
public sealed record ReportDateRange(DateOnly FromDate, DateOnly ToDate)
{
    public static ReportDateRange Resolve(DateOnly? fromDate, DateOnly? toDate, DateOnly today)
    {
        var to = toDate ?? today;
        var from = fromDate ?? new DateOnly(to.Year, 1, 1);

        if (from > to)
        {
            throw new ArgumentException("FromDate must be on or before ToDate.");
        }

        return new ReportDateRange(from, to);
    }

    public IEnumerable<DateOnly> Months()
    {
        var cursor = new DateOnly(FromDate.Year, FromDate.Month, 1);
        var end = new DateOnly(ToDate.Year, ToDate.Month, 1);

        while (cursor <= end)
        {
            yield return cursor;
            cursor = cursor.AddMonths(1);
        }
    }
}
