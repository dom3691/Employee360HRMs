namespace Employee360.Application.Features.Performance;

/// <summary>
/// Weighted final rating calculation (FR-PERF-005).
/// Goals are scored 1-5 from achievement; blended with self, manager, and peer ratings.
/// </summary>
public static class WeightedRatingCalculator
{
    public sealed record Input(
        decimal GoalWeightPercent,
        decimal SelfWeightPercent,
        decimal ManagerWeightPercent,
        decimal PeerWeightPercent,
        decimal? SelfRating,
        decimal? ManagerRating,
        decimal? PeerAverageRating,
        IReadOnlyList<GoalInput> Goals);

    public sealed record GoalInput(decimal Weight, decimal TargetValue, decimal? ActualValue);

    public sealed record Result(
        decimal GoalScore,
        decimal WeightedFinalRating,
        decimal GoalAchievementPercent);

    /// <summary>
    /// Calculates weighted final rating on a 1-5 scale.
    /// Goal score = weighted average of min(actual/target, 1) * 5 per goal.
    /// </summary>
    public static Result Calculate(Input input)
    {
        var goalScore = CalculateGoalScore(input.Goals);
        var goalAchievement = CalculateGoalAchievementPercent(input.Goals);

        var components = new List<(decimal weight, decimal? rating)>
        {
            (input.GoalWeightPercent, goalScore),
            (input.SelfWeightPercent, input.SelfRating),
            (input.ManagerWeightPercent, input.ManagerRating),
            (input.PeerWeightPercent, input.PeerAverageRating),
        };

        var applicableWeight = components
            .Where(c => c.rating.HasValue && c.weight > 0)
            .Sum(c => c.weight);

        if (applicableWeight <= 0)
        {
            return new Result(goalScore, 0m, goalAchievement);
        }

        var weightedSum = components
            .Where(c => c.rating.HasValue && c.weight > 0)
            .Sum(c => c.weight * c.rating!.Value);

        var finalRating = Math.Round(weightedSum / applicableWeight, 2, MidpointRounding.AwayFromZero);

        return new Result(
            Math.Round(goalScore, 2, MidpointRounding.AwayFromZero),
            finalRating,
            goalAchievement);
    }

    public static decimal CalculateGoalScore(IReadOnlyList<GoalInput> goals)
    {
        if (goals.Count == 0)
        {
            return 0m;
        }

        var totalWeight = goals.Sum(g => g.Weight);
        if (totalWeight <= 0)
        {
            return 0m;
        }

        var weightedAchievement = goals.Sum(g =>
        {
            var achievement = g.TargetValue > 0 && g.ActualValue.HasValue
                ? Math.Min(g.ActualValue.Value / g.TargetValue, 1m)
                : 0m;

            return g.Weight * achievement * 5m;
        });

        return weightedAchievement / totalWeight;
    }

    public static decimal CalculateGoalAchievementPercent(IReadOnlyList<GoalInput> goals)
    {
        if (goals.Count == 0)
        {
            return 0m;
        }

        var totalWeight = goals.Sum(g => g.Weight);
        if (totalWeight <= 0)
        {
            return 0m;
        }

        var weighted = goals.Sum(g =>
        {
            var achievement = g.TargetValue > 0 && g.ActualValue.HasValue
                ? Math.Min(g.ActualValue.Value / g.TargetValue, 1m) * 100m
                : 0m;

            return g.Weight * achievement;
        });

        return Math.Round(weighted / totalWeight, 2, MidpointRounding.AwayFromZero);
    }
}
