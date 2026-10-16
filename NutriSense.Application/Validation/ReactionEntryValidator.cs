using NutriSense.Domain.Common;

namespace NutriSense.Application.Validation;

/// <summary>
/// Validates user input when recording an adverse symptom or reaction.
/// </summary>
public sealed class ReactionEntryValidator
{
    public Result Validate(string? symptomType, int severity, DateTime occurredAtUtc)
    {
        if (string.IsNullOrWhiteSpace(symptomType))
        {
            return Result.Failure(Error.Validation("ReactionValidation.EmptySymptom", "Symptom type is required."));
        }

        if (severity is < 1 or > 10)
        {
            return Result.Failure(Error.Validation("ReactionValidation.InvalidSeverity", "Severity must be between 1 (mild discomfort) and 10 (emergency/severe)."));
        }

        if (occurredAtUtc > DateTime.UtcNow.AddMinutes(5))
        {
            return Result.Failure(Error.Validation("ReactionValidation.FutureDate", "Reaction time cannot be in the future."));
        }

        return Result.Success();
    }
}
