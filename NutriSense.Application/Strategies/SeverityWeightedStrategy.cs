using NutriSense.Domain.Entities;
using NutriSense.Domain.Strategies;

namespace NutriSense.Application.Strategies;

/// <summary>
/// Calculates an acute risk index by weighting symptom occurrences by recorded severity (1-10).
/// </summary>
public sealed class SeverityWeightedStrategy : IReactionAnalysisStrategy
{
    public string StrategyName => "Severity-Weighted Impact Analysis";
    public string Description => "Calculates an acute risk index by weighting symptom occurrences by recorded severity (1-10).";

    public IReadOnlyList<TriggerInsight> Analyze(
        IReadOnlyList<FoodEntry> foods,
        IReadOnlyList<ReactionEntry> reactions,
        TimeSpan timeWindow)
    {
        var insights = new List<TriggerInsight>();
        var foodsByName = foods.GroupBy(f => f.FoodName, StringComparer.OrdinalIgnoreCase);

        foreach (var foodGroup in foodsByName)
        {
            var foodName = foodGroup.Key;
            var totalTimes = foodGroup.Count();
            var symptomTypes = reactions.Select(r => r.SymptomType).Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var symptom in symptomTypes)
            {
                var correlatedSeverities = new List<int>();

                foreach (var meal in foodGroup)
                {
                    var matchingReactions = reactions.Where(r =>
                        string.Equals(r.SymptomType, symptom, StringComparison.OrdinalIgnoreCase) &&
                        r.OccurredAtUtc >= meal.ConsumedAtUtc &&
                        r.OccurredAtUtc <= meal.ConsumedAtUtc.Add(timeWindow)).ToList();

                    if (matchingReactions.Count > 0)
                    {
                        correlatedSeverities.AddRange(matchingReactions.Select(r => r.Severity));
                    }
                }

                if (correlatedSeverities.Count > 0)
                {
                    var occurrences = correlatedSeverities.Count;
                    var avgSeverity = correlatedSeverities.Average();
                    var correlationRate = (double)occurrences / totalTimes;

                    var compositeRiskScore = Math.Round(correlationRate * avgSeverity, 1);
                    var percentage = Math.Round(correlationRate * 100, 1);

                    var severityLevel = compositeRiskScore switch
                    {
                        >= 7.0 => "Critical",
                        >= 4.5 => "High",
                        >= 2.5 => "Moderate",
                        _ => "Low"
                    };

                    var recommendation = compositeRiskScore >= 6.0
                        ? $"URGENT TRIGGER: Triggers severe {symptom} (Avg severity {avgSeverity:F1}/10). Immediate elimination recommended."
                        : $"Moderate trigger: Triggers {symptom} with avg severity {avgSeverity:F1}/10.";

                    insights.Add(new TriggerInsight(
                        foodName,
                        symptom,
                        occurrences,
                        totalTimes,
                        percentage,
                        compositeRiskScore,
                        severityLevel,
                        recommendation));
                }
            }
        }

        return insights.OrderByDescending(i => i.RiskScore).ToList();
    }
}
