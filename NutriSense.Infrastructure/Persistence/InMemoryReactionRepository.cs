using NutriSense.Domain.Entities;
using NutriSense.Domain.Repositories;

namespace NutriSense.Infrastructure.Persistence;

/// <summary>
/// In-memory repository managing adverse reaction entries.
/// Pre-seeded with sample data to demonstrate correlation patterns.
/// </summary>
public sealed class InMemoryReactionRepository : IReactionRepository
{
    private readonly List<ReactionEntry> _entries = [];
    private readonly object _lock = new();

    public InMemoryReactionRepository(Guid defaultUserId)
    {
        SeedSampleData(defaultUserId);
    }

    private void SeedSampleData(Guid userId)
    {
        var now = DateTime.UtcNow;

        // Day 1: Severe Heartburn 2h after Spicy Jalapeno Burger
        AddSeed(userId, "Heartburn / Acid Reflux", 8, now.AddDays(-7).AddHours(15), 90, "Severe burning in chest after burger");

        // Day 2: Acne Breakout morning after Pepperoni Pizza
        AddSeed(userId, "Acne Flare-up", 7, now.AddDays(-5).AddHours(8).AddMinutes(30), 480, "Red inflammatory pimples on forehead and chin");

        // Day 3: Heartburn after Thai Red Curry
        AddSeed(userId, "Heartburn / Acid Reflux", 9, now.AddDays(-5).AddHours(14).AddMinutes(30), 120, "Intense acid regurgitation and chest discomfort");

        // Day 3: Migraine / Crash 2.5h after Monster Energy Drink
        AddSeed(userId, "Migraine & Crash", 8, now.AddDays(-5).AddHours(18).AddMinutes(30), 180, "Throbbing headache on temples after sugar crash");

        // Day 5: Heartburn after Buffalo Wings
        AddSeed(userId, "Heartburn / Acid Reflux", 7, now.AddDays(-3).AddHours(15).AddMinutes(45), 90, "Burning sensation again");

        // Day 6: Acne Flare-up following Cheesecake & Quesadilla
        AddSeed(userId, "Acne Flare-up", 8, now.AddDays(-2).AddHours(9), 720, "Cystic acne flare-up after heavy dairy");

        // Day 6: Headache after Monster Energy Drink
        AddSeed(userId, "Migraine & Crash", 6, now.AddDays(-2).AddHours(13).AddMinutes(30), 120, "Temple pressure headache");

        // Day 7: Acne Flare-up morning after Pepperoni Pizza
        AddSeed(userId, "Acne Flare-up", 7, now.AddHours(-3), 360, "New breakout on cheeks");
    }

    private void AddSeed(Guid userId, string symptom, int severity, DateTime time, int duration, string notes)
    {
        var result = ReactionEntry.Create(userId, symptom, severity, time, duration, notes);
        if (result.IsSuccess)
        {
            _entries.Add(result.Value);
        }
    }

    public Task<IReadOnlyList<ReactionEntry>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var list = _entries.Where(e => e.UserId == userId).OrderByDescending(e => e.OccurredAtUtc).ToList();
            return Task.FromResult<IReadOnlyList<ReactionEntry>>(list);
        }
    }

    public Task<ReactionEntry?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var entry = _entries.FirstOrDefault(e => e.Id == id && e.UserId == userId);
            return Task.FromResult(entry);
        }
    }

    public Task AddAsync(ReactionEntry entry, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _entries.Add(entry);
            return Task.CompletedTask;
        }
    }

    public Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _entries.RemoveAll(e => e.Id == id && e.UserId == userId);
            return Task.CompletedTask;
        }
    }
}
