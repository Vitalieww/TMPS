using NutriSense.Domain.Entities;
using NutriSense.Domain.Strategies;

namespace NutriSense.Application.Strategies;

/// <summary>
/// Extracts common ingredients across meals and calculates cross-dish trigger correlations.
/// </summary>
public sealed class IngredientIntoleranceStrategy : IReactionAnalysisStrategy
{
    public string StrategyName => "Ingredient-Level Allergen Analysis";
    public string Description => "Extracts common underlying ingredients (dairy, gluten, caffeine, etc.) across different foods and computes cross-dish trigger correlations.";

    public IReadOnlyList<TriggerInsight> Analyze(
        IReadOnlyList<FoodEntry> foods,
        IReadOnlyList<ReactionEntry> reactions,
        TimeSpan timeWindow)
    {
        var insights = new List<TriggerInsight>();

        var allIngredients = foods
            .SelectMany(f => f.Ingredients)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var symptomTypes = reactions.Select(r => r.SymptomType).Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var ingredient in allIngredients)
        {
            var mealsWithIngredient = foods.Where(f => f.Ingredients.Contains(ingredient, StringComparer.OrdinalIgnoreCase)).ToList();
            var totalMeals = mealsWithIngredient.Count;

            if (totalMeals < 2) continue;

            foreach (var symptom in symptomTypes)
            {
                var timesPreceded = 0;

                foreach (var meal in mealsWithIngredient)
                {
                    var hasReaction = reactions.Any(r =>
                        string.Equals(r.SymptomType, symptom, StringComparison.OrdinalIgnoreCase) &&
                        r.OccurredAtUtc >= meal.ConsumedAtUtc &&
                        r.OccurredAtUtc <= meal.ConsumedAtUtc.Add(timeWindow));

                    if (hasReaction) timesPreceded++;
                }

                if (timesPreceded > 0)
                {
                    var percentage = Math.Round((double)timesPreceded / totalMeals * 100, 1);
                    var riskScore = Math.Round(percentage / 10, 1);
                    var severityLevel = percentage >= 70 ? "Critical" : percentage >= 40 ? "High" : "Moderate";

                    insights.Add(new TriggerInsight(
                        $"[Ingredient] {ingredient.ToUpperInvariant()}",
                        symptom,
                        timesPreceded,
                        totalMeals,
                        percentage,
                        riskScore,
                        severityLevel,
                        $"Found in {totalMeals} meals. Triggers {symptom} {percentage}% of the time. High probability of systemic sensitivity."));
                }
            }
        }

        return insights.OrderByDescending(i => i.CorrelationPercentage).ToList();
    }
}
