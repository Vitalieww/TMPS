using NutriSense.Domain.Common;

namespace NutriSense.Domain.Entities;

/// <summary>
/// Represents a logged meal consumed by a user.
/// </summary>
public sealed class FoodEntry
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string FoodName { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public IReadOnlyList<string> Ingredients { get; private set; } = [];
    public int Rating { get; private set; }
    public DateTime ConsumedAtUtc { get; private set; }
    public string Notes { get; private set; } = string.Empty;

    private FoodEntry() { }

    public static Result<FoodEntry> Create(
        Guid userId,
        string foodName,
        string category,
        IEnumerable<string>? ingredients,
        int rating,
        DateTime consumedAtUtc,
        string notes = "")
    {
        if (userId == Guid.Empty)
            return Result.Failure<FoodEntry>(Error.Validation("FoodEntry.InvalidUserId", "User ID is required."));

        if (string.IsNullOrWhiteSpace(foodName))
            return Result.Failure<FoodEntry>(Error.Validation("FoodEntry.EmptyName", "Food name cannot be empty."));

        if (rating is < 1 or > 5)
            return Result.Failure<FoodEntry>(Error.Validation("FoodEntry.InvalidRating", "Rating must be between 1 and 5."));

        var entry = new FoodEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FoodName = foodName.Trim(),
            Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim(),
            Ingredients = ingredients?.Select(i => i.Trim().ToLowerInvariant()).Where(i => !string.IsNullOrEmpty(i)).ToList() ?? [],
            Rating = rating,
            ConsumedAtUtc = consumedAtUtc.Kind == DateTimeKind.Utc ? consumedAtUtc : consumedAtUtc.ToUniversalTime(),
            Notes = notes?.Trim() ?? string.Empty
        };

        return Result.Success(entry);
    }
}
