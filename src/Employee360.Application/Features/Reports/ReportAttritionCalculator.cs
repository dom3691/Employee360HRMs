using Employee360.Domain.Enums;

namespace Employee360.Application.Features.Reports;

/// <summary>
/// Attrition rate calculations (FR-RPT-003).
/// Voluntary = Resigned; Involuntary = Terminated.
/// </summary>
public static class ReportAttritionCalculator
{
    public sealed record AttritionMetrics(
        int OpeningHeadcount,
        int ClosingHeadcount,
        int VoluntaryExits,
        int InvoluntaryExits,
        decimal AverageHeadcount,
        decimal AttritionRatePercent,
        decimal VoluntaryRatePercent,
        decimal InvoluntaryRatePercent);

    public static AttritionMetrics Calculate(
        int openingHeadcount,
        int closingHeadcount,
        int voluntaryExits,
        int involuntaryExits)
    {
        var totalExits = voluntaryExits + involuntaryExits;
        var average = (openingHeadcount + closingHeadcount) / 2m;
        var denominator = average > 0 ? average : 1m;

        return new AttritionMetrics(
            openingHeadcount,
            closingHeadcount,
            voluntaryExits,
            involuntaryExits,
            average,
            RoundPercent(totalExits / denominator * 100m),
            RoundPercent(voluntaryExits / denominator * 100m),
            RoundPercent(involuntaryExits / denominator * 100m));
    }

    public static bool IsExitStatus(EmployeeStatus status) =>
        status is EmployeeStatus.Resigned or EmployeeStatus.Terminated;

    public static bool IsVoluntary(EmployeeStatus status) =>
        status == EmployeeStatus.Resigned;

    public static bool IsActiveHeadcount(EmployeeStatus status) =>
        status is EmployeeStatus.Active or EmployeeStatus.OnLeave or EmployeeStatus.Suspended;

    private static decimal RoundPercent(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
