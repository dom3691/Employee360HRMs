using Employee360.Infrastructure.Services;
using FluentAssertions;

namespace Employee360.UnitTests.Infrastructure;

/// <summary>Tests for the WAT (UTC+1) clock implementation.</summary>
public class DateTimeProviderTests
{
    private readonly DateTimeProvider _provider = new();

    [Fact]
    public void UtcNow_ShouldBeCloseToSystemUtcNow()
    {
        _provider.UtcNow.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void WatNow_ShouldBeUtcPlusOneHour()
    {
        var wat = _provider.WatNow;

        wat.Offset.Should().Be(TimeSpan.FromHours(1));
        wat.UtcDateTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void TodayWat_ShouldMatchWatCalendarDate()
    {
        _provider.TodayWat.Should().Be(DateOnly.FromDateTime(_provider.WatNow.DateTime));
    }
}
