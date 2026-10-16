using NutriSense.Application.Validation;
using NutriSense.Domain.Common;
using NutriSense.Domain.Entities;
using NutriSense.Domain.Repositories;

namespace NutriSense.Application.Services;

/// <summary>
/// Application service coordinating food tracking, adverse symptom logging, and correlation insights.
/// </summary>
public sealed class NutriSenseTrackerService
{
    private readonly IFoodRepository _foodRepository;
    private readonly IReactionRepository _reactionRepository;
    private readonly FoodEntryValidator _foodValidator;
    private readonly ReactionEntryValidator _reactionValidator;
    private readonly CorrelationEngine _correlationEngine;

    public NutriSenseTrackerService(
        IFoodRepository foodRepository,
        IReactionRepository reactionRepository,
        FoodEntryValidator foodValidator,
        ReactionEntryValidator reactionValidator,
        CorrelationEngine correlationEngine)
    {
        _foodRepository = foodRepository ?? throw new ArgumentNullException(nameof(foodRepository));
        _reactionRepository = reactionRepository ?? throw new ArgumentNullException(nameof(reactionRepository));
        _foodValidator = foodValidator ?? throw new ArgumentNullException(nameof(foodValidator));
        _reactionValidator = reactionValidator ?? throw new ArgumentNullException(nameof(reactionValidator));
        _correlationEngine = correlationEngine ?? throw new ArgumentNullException(nameof(correlationEngine));
    }

    public async Task<Result<Guid>> LogFoodAsync(
        Guid userId,
        string foodName,
        string category,
        IEnumerable<string> ingredients,
        int rating,
        DateTime consumedAtUtc,
        string notes = "",
        CancellationToken ct = default)
    {
        var validation = _foodValidator.Validate(foodName, rating, consumedAtUtc);
        if (validation.IsFailure)
        {
            return Result.Failure<Guid>(validation.Error);
        }
        
        var entryResult = FoodEntry.Create(userId, foodName, category, ingredients, rating, consumedAtUtc, notes);
        if (entryResult.IsFailure)
        {
            return Result.Failure<Guid>(entryResult.Error);
        }

        await _foodRepository.AddAsync(entryResult.Value, ct);
        return Result.Success(entryResult.Value.Id);
    }

    public async Task<Result<Guid>> LogReactionAsync(
        Guid userId,
        string symptomType,
        int severity,
        DateTime occurredAtUtc,
        int? durationMinutes = null,
        string notes = "",
        CancellationToken ct = default)
    {
        var validation = _reactionValidator.Validate(symptomType, severity, occurredAtUtc);
        if (validation.IsFailure)
        {
            return Result.Failure<Guid>(validation.Error);
        }

        var entryResult = ReactionEntry.Create(userId, symptomType, severity, occurredAtUtc, durationMinutes, notes);
        if (entryResult.IsFailure)
        {
            return Result.Failure<Guid>(entryResult.Error);
        }

        await _reactionRepository.AddAsync(entryResult.Value, ct);
        return Result.Success(entryResult.Value.Id);
    }

    public async Task<IReadOnlyList<FoodEntry>> GetFoodHistoryAsync(Guid userId, CancellationToken ct = default) =>
        await _foodRepository.GetAllByUserIdAsync(userId, ct);

    public async Task<IReadOnlyList<ReactionEntry>> GetReactionHistoryAsync(Guid userId, CancellationToken ct = default) =>
        await _reactionRepository.GetAllByUserIdAsync(userId, ct);

    public async Task<Dictionary<string, IReadOnlyList<TriggerInsight>>> AnalyzeTriggersAsync(
        Guid userId,
        TimeSpan timeWindow,
        CancellationToken ct = default)
    {
        var foods = await _foodRepository.GetAllByUserIdAsync(userId, ct);
        var reactions = await _reactionRepository.GetAllByUserIdAsync(userId, ct);

        return _correlationEngine.RunAllStrategies(foods, reactions, timeWindow);
    }
}
