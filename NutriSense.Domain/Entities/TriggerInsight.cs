namespace NutriSense.Domain.Entities;

/// <summary>
/// Domain Model representing a calculated statistical correlation between a food and a symptom.
/// </summary>
public sealed record TriggerInsight(
    string FoodName,
    string SymptomType,
    int TimesConsumedBeforeSymptom,
    int TotalTimesConsumed,
    double CorrelationPercentage,
    double RiskScore,
    string SeverityLevel, // "Low", "Moderate", "High", "Critical"
    string SummaryRecommendation);
