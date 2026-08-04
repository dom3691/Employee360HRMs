using Employee360.Application.Features.Reports;
using Employee360.Domain.Enums;
using FluentAssertions;

namespace Employee360.UnitTests.Application.Reports;

public class ReportAttritionCalculatorTests
{
    [Fact]
    public void Calculate_AttritionRate_UsesAverageHeadcountAsDenominator()
    {
        var metrics = ReportAttritionCalculator.Calculate(
            openingHeadcount: 100,
            closingHeadcount: 80,
            voluntaryExits: 10,
            involuntaryExits: 10);

        metrics.AverageHeadcount.Should().Be(90m);
        metrics.AttritionRatePercent.Should().Be(22.22m);
        metrics.VoluntaryRatePercent.Should().Be(11.11m);
        metrics.InvoluntaryRatePercent.Should().Be(11.11m);
    }

    [Fact]
    public void Calculate_ZeroAverageHeadcount_DoesNotDivideByZero()
    {
        var metrics = ReportAttritionCalculator.Calculate(0, 0, 2, 1);

        metrics.AttritionRatePercent.Should().Be(300m);
    }

    [Theory]
    [InlineData(EmployeeStatus.Resigned, true)]
    [InlineData(EmployeeStatus.Terminated, false)]
    public void IsVoluntary_ClassifiesExitTypes(
        EmployeeStatus status,
        bool expectedVoluntary)
    {
        ReportAttritionCalculator.IsVoluntary(status).Should().Be(expectedVoluntary);
    }
}
