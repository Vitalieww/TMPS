using System.Text;
using NutriSense.Domain.Entities;

namespace NutriSense.Infrastructure.Reporting;

/// <summary>
/// Formats correlation analysis output for terminal/console display.
/// </summary>
public sealed class ConsoleReportFormatter : IReportFormatter
{
    public string FormatInsights(string strategyName, IReadOnlyList<TriggerInsight> insights)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"\n=== {strategyName.ToUpperInvariant()} ===");
        sb.AppendLine(new string('-', 85));
        sb.AppendLine($"{"FOOD / INGREDIENT",-28} | {"CORRELATED SYMPTOM",-24} | {"RATE",-8} | {"RISK",-8} | {"LEVEL",-10}");
        sb.AppendLine(new string('-', 85));

        if (insights.Count == 0)
        {
            sb.AppendLine(" No correlated trigger patterns identified within this time window.");
            return sb.ToString();
        }

        foreach (var item in insights)
        {
            var rateStr = $"{item.CorrelationPercentage:F0}%";
            var riskStr = $"{item.RiskScore:F1}/10";

            sb.AppendLine($"{Truncate(item.FoodName, 28),-28} | {Truncate(item.SymptomType, 24),-24} | {rateStr,-8} | {riskStr,-8} | {item.SeverityLevel,-10}");
            sb.AppendLine($"  ↳ Recommendation: {item.SummaryRecommendation}");
        }

        sb.AppendLine(new string('-', 85));
        return sb.ToString();
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..(max - 3)] + "...";
}
