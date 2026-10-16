using NutriSense.Domain.Common;

namespace NutriSense.Application.Validation;

/// <summary>
/// Validates user input when logging a food entry.
/// </summary>
public sealed class FoodEntryValidator
{
    public Result Validate(string? foodName, int rating, DateTime consumedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(foodName))
        {
            return Result.Failure(Error.Validation("FoodValidation.EmptyName", "Food item name is required."));
        }

        if (foodName.Trim().Length < 2)
        {
            return Result.Failure(Error.Validation("FoodValidation.NameTooShort", "Food name must be at least 2 characters long."));
        }

        if (foodName.Trim().Length > 100)
        {
            return Result.Failure(Error.Validation("FoodValidation.NameTooLong", "Food name cannot exceed 100 characters."));
        }

        if (rating is < 1 or > 5)
        {
            return Result.Failure(Error.Validation("FoodValidation.InvalidRating", "Rating must be between 1 and 5 stars."));
        }

        if (consumedAtUtc > DateTime.UtcNow.AddMinutes(5))
        {
            return Result.Failure(Error.Validation("FoodValidation.FutureDate", "Meal consumption time cannot be in the future."));
        }

        return Result.Success();
    }
}
