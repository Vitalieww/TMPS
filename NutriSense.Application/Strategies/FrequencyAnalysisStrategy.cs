using NutriSense.Domain.Entities;
using NutriSense.Domain.Strategies;

namespace NutriSense.Application.Strategies;

/// <summary>
/// Calculates the raw recurrence rate of adverse reactions occurring within a time window following a meal.
/// </summary>
public sealed class FrequencyAnalysisStrategy : IReactionAnalysisStrategy
{
    public string StrategyName => "Frequency Correlation Analysis";
    public string Description => "Measures the raw recurrence rate of symptoms occurring within 2-12h following food ingestion.";

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
            var totalTimesConsumed = foodGroup.Count();
            var symptomTypes = reactions.Select(r => r.SymptomType).Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var symptom in symptomTypes)
            {
                var timesPrecededSymptom = 0;

                foreach (var meal in foodGroup)
                {
                    var hasCorrelatedReaction = reactions.Any(r =>
                        string.Equals(r.SymptomType, symptom, StringComparison.OrdinalIgnoreCase) &&
                        r.OccurredAtUtc >= meal.ConsumedAtUtc &&
                        r.OccurredAtUtc <= meal.ConsumedAtUtc.Add(timeWindow));

                    if (hasCorrelatedReaction)
                    {
                        timesPrecededSymptom++;
                    }
                }

                if (timesPrecededSymptom > 0)
                {
                    var percentage = Math.Round((double)timesPrecededSymptom / totalTimesConsumed * 100, 1);
                    var riskScore = Math.Round(percentage / 10, 1);
                    var severityLevel = percentage switch
                    {
                        >= 75 => "Critical",
                        >= 50 => "High",
                        >= 25 => "Moderate",
                        _ => "Low"
                    };

                    var recommendation = percentage >= 60
                        ? $"Strong intolerance suspect. Consider an elimination trial of '{foodName}' for 14 days."
                        : $"Mild correlation detected with '{symptom}'. Monitor further consumption.";

                    insights.Add(new TriggerInsight(
                        foodName,
                        symptom,
                        timesPrecededSymptom,
                        totalTimesConsumed,
                        percentage,
                        riskScore,
                        severityLevel,
                        recommendation));
                }
            }
        }

        return insights.OrderByDescending(i => i.CorrelationPercentage).ToList();
    }
}
