using NutriSense.Domain.Entities;

namespace NutriSense.Domain.Strategies;

/// <summary>
/// Defines a correlation analysis strategy for identifying food-symptom relationships.
/// </summary>
public interface IReactionAnalysisStrategy
{
    string StrategyName { get; }
    string Description { get; }

    IReadOnlyList<TriggerInsight> Analyze(
        IReadOnlyList<FoodEntry> foods,
        IReadOnlyList<ReactionEntry> reactions,
        TimeSpan timeWindow);
}
