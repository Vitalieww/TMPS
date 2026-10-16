using NutriSense.Domain.Common;

namespace NutriSense.Domain.Entities;

/// <summary>
/// Represents an adverse reaction or health symptom experienced by a user.
/// </summary>
public sealed class ReactionEntry
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string SymptomType { get; private set; } = string.Empty;
    public int Severity { get; private set; } // 1 - 10 scale
    public DateTime OccurredAtUtc { get; private set; }
    public int? DurationMinutes { get; private set; }
    public string Notes { get; private set; } = string.Empty;

    private ReactionEntry() { }

    public static Result<ReactionEntry> Create(
        Guid userId,
        string symptomType,
        int severity,
        DateTime occurredAtUtc,
        int? durationMinutes = null,
        string notes = "")
    {
        if (userId == Guid.Empty)
            return Result.Failure<ReactionEntry>(Error.Validation("ReactionEntry.InvalidUserId", "User ID is required."));

        if (string.IsNullOrWhiteSpace(symptomType))
            return Result.Failure<ReactionEntry>(Error.Validation("ReactionEntry.EmptySymptom", "Symptom type cannot be empty."));

        if (severity is < 1 or > 10)
            return Result.Failure<ReactionEntry>(Error.Validation("ReactionEntry.InvalidSeverity", "Severity must be between 1 (mild) and 10 (severe)."));

        var entry = new ReactionEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SymptomType = symptomType.Trim(),
            Severity = severity,
            OccurredAtUtc = occurredAtUtc.Kind == DateTimeKind.Utc ? occurredAtUtc : occurredAtUtc.ToUniversalTime(),
            DurationMinutes = durationMinutes,
            Notes = notes?.Trim() ?? string.Empty
        };

        return Result.Success(entry);
    }
}
