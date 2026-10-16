using NutriSense.Domain.Entities;

namespace NutriSense.Infrastructure.Reporting;

/// <summary>
/// Contract for rendering trigger correlation reports.
/// </summary>
public interface IReportFormatter
{
    string FormatInsights(string strategyName, IReadOnlyList<TriggerInsight> insights);
}
