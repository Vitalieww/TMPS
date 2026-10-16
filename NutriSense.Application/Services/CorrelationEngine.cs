using NutriSense.Domain.Entities;
using NutriSense.Domain.Strategies;

namespace NutriSense.Application.Services;

/// <summary>
/// Orchestrates execution of registered correlation analysis strategies.
/// </summary>
public sealed class CorrelationEngine
{
    private readonly IEnumerable<IReactionAnalysisStrategy> _strategies;

    public CorrelationEngine(IEnumerable<IReactionAnalysisStrategy> strategies)
    {
        _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
    }

    public IReadOnlyList<IReactionAnalysisStrategy> AvailableStrategies => _strategies.ToList();

    public Dictionary<string, IReadOnlyList<TriggerInsight>> RunAllStrategies(
        IReadOnlyList<FoodEntry> foods,
        IReadOnlyList<ReactionEntry> reactions,
        TimeSpan timeWindow)
    {
        var results = new Dictionary<string, IReadOnlyList<TriggerInsight>>();

        foreach (var strategy in _strategies)
        {
            var insights = strategy.Analyze(foods, reactions, timeWindow);
            results[strategy.StrategyName] = insights;
        }

        return results;
    }

    public IReadOnlyList<TriggerInsight> RunStrategy(
        string strategyName,
        IReadOnlyList<FoodEntry> foods,
        IReadOnlyList<ReactionEntry> reactions,
        TimeSpan timeWindow)
    {
        var strategy = _strategies.FirstOrDefault(s =>
            string.Equals(s.StrategyName, strategyName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Strategy '{strategyName}' is not registered.");

        return strategy.Analyze(foods, reactions, timeWindow);
    }
}
