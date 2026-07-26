namespace Employee360.Application.Common.Interfaces;

/// <summary>
/// Calculates working days between two dates, excluding weekends and configured
/// public holidays (PRD FR-LV-003).
/// </summary>
public interface IWorkingDaysCalculator
{
    /// <summary>
    /// Counts working days from <paramref name="startDate"/> to
    /// <paramref name="endDate"/> inclusive.
    /// </summary>
    /// <param name="startDate">First day.</param>
    /// <param name="endDate">Last day (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<decimal> CalculateAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
}
