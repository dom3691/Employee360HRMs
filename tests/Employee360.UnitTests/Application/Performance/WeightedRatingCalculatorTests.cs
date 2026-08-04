using Employee360.Application.Features.Performance;
using FluentAssertions;

namespace Employee360.UnitTests.Application.Performance;

public class WeightedRatingCalculatorTests
{
    [Fact]
    public void Calculate_BlendsGoalSelfManagerAndPeerWeights()
    {
        var result = WeightedRatingCalculator.Calculate(new WeightedRatingCalculator.Input(
            GoalWeightPercent: 70m,
            SelfWeightPercent: 15m,
            ManagerWeightPercent: 15m,
            PeerWeightPercent: 0m,
            SelfRating: 4m,
            ManagerRating: 5m,
            PeerAverageRating: null,
            Goals:
            [
                new WeightedRatingCalculator.GoalInput(60m, 100m, 80m),
                new WeightedRatingCalculator.GoalInput(40m, 50m, 50m),
            ]));

        // Goal score: (60*0.8*5 + 40*1.0*5)/100 = 4.4
        result.GoalScore.Should().Be(4.4m);
        // Final: (70*4.4 + 15*4 + 15*5) / 100 = 4.43
        result.WeightedFinalRating.Should().Be(4.43m);
    }

    [Fact]
    public void CalculateGoalScore_ReturnsZeroWhenNoGoals()
    {
        WeightedRatingCalculator.CalculateGoalScore([]).Should().Be(0m);
    }

    [Fact]
    public void Calculate_IncludesPeerRatingWhenConfigured()
    {
        var result = WeightedRatingCalculator.Calculate(new WeightedRatingCalculator.Input(
            50m, 20m, 20m, 10m,
            SelfRating: 3m,
            ManagerRating: 4m,
            PeerAverageRating: 5m,
            Goals: [new WeightedRatingCalculator.GoalInput(100m, 100m, 100m)]));

        // Goal score = 5, final = (50*5 + 20*3 + 20*4 + 10*5)/100 = 4.4
        result.WeightedFinalRating.Should().Be(4.4m);
    }
}
