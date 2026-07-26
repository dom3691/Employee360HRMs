using Employee360.Domain.Interfaces;

namespace Employee360.Infrastructure.Services;

/// <summary>
/// System clock implementation. West Africa Time is a fixed UTC+1 offset with no
/// daylight saving, so the conversion is a constant offset.
/// </summary>
public sealed class DateTimeProvider : IDateTimeProvider
{
    private static readonly TimeSpan WatOffset = TimeSpan.FromHours(1);

    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;

    /// <inheritdoc />
    public DateTimeOffset WatNow => new DateTimeOffset(DateTime.UtcNow).ToOffset(WatOffset);

    /// <inheritdoc />
    public DateOnly TodayWat => DateOnly.FromDateTime(WatNow.DateTime);
}
