using NutriSense.Domain.Entities;
using NutriSense.Domain.Repositories;

namespace NutriSense.Infrastructure.Persistence;

/// <summary>
/// In-memory repository implementation managing food entries.
/// Pre-seeded with sample data to demonstrate trigger patterns immediately.
/// </summary>
public sealed class InMemoryFoodRepository : IFoodRepository
{
    private readonly List<FoodEntry> _entries = [];
    private readonly object _lock = new();

    public InMemoryFoodRepository(Guid defaultUserId)
    {
        SeedSampleData(defaultUserId);
    }

    private void SeedSampleData(Guid userId)
    {
        var now = DateTime.UtcNow;

        // Day 1
        AddSeed(userId, "Espresso Double Shot", "Beverages", ["caffeine", "coffee"], 4, now.AddDays(-7).AddHours(8), "Morning kick");
        AddSeed(userId, "Oatmeal with Almond Milk", "Breakfast", ["oats", "almond milk", "berries"], 5, now.AddDays(-7).AddHours(9), "Safe clean meal");
        AddSeed(userId, "Spicy Jalapeno Burger", "Fast Food", ["beef", "cheese", "dairy", "gluten", "spicy", "jalapeno"], 5, now.AddDays(-7).AddHours(13), "Cheesy and very spicy");
        AddSeed(userId, "Steamed Salmon & Rice", "Dinner", ["salmon", "rice", "olive oil"], 4, now.AddDays(-7).AddHours(19), "Felt fine");

        // Day 2
        AddSeed(userId, "Greek Yogurt with Honey", "Dairy", ["dairy", "lactose", "honey"], 4, now.AddDays(-6).AddHours(9), "Quick breakfast");
        AddSeed(userId, "Chicken Salad", "Lunch", ["chicken", "greens", "olive oil"], 4, now.AddDays(-6).AddHours(13), "Clean lunch");
        AddSeed(userId, "Extra Cheese Pepperoni Pizza", "Fast Food", ["cheese", "dairy", "gluten", "pork", "tomato"], 5, now.AddDays(-6).AddHours(20), "Heavy dinner with friends");

        // Day 3
        AddSeed(userId, "Espresso Double Shot", "Beverages", ["caffeine", "coffee"], 4, now.AddDays(-5).AddHours(8), "Needed focus");
        AddSeed(userId, "Spicy Thai Red Curry", "Lunch", ["coconut milk", "spicy", "chili", "chicken"], 5, now.AddDays(-5).AddHours(13), "Very hot spice");
        AddSeed(userId, "Monster Energy Drink", "Beverages", ["caffeine", "high-sugar", "taurine"], 3, now.AddDays(-5).AddHours(16), "Afternoon boost");

        // Day 4
        AddSeed(userId, "Cheese Quesadilla", "Fast Food", ["cheese", "dairy", "tortilla", "gluten"], 4, now.AddDays(-4).AddHours(13), "Quick snack");
        AddSeed(userId, "Quinoa & Grilled Tofu", "Dinner", ["quinoa", "tofu", "vegetables"], 3, now.AddDays(-4).AddHours(19), "Trying plant-based");

        // Day 5
        AddSeed(userId, "Spicy Buffalo Wings", "Fast Food", ["chicken", "spicy", "hot sauce", "butter", "dairy"], 5, now.AddDays(-3).AddHours(14), "Spicy lunch");
        AddSeed(userId, "Chocolate Cheesecake", "Dessert", ["cheese", "dairy", "sugar", "gluten"], 5, now.AddDays(-3).AddHours(17), "Birthday slice");

        // Day 6
        AddSeed(userId, "Monster Energy Drink", "Beverages", ["caffeine", "high-sugar"], 3, now.AddDays(-2).AddHours(11), "Studying for exam");
        AddSeed(userId, "Rice Bowl with Veggies", "Lunch", ["rice", "broccoli", "carrots"], 4, now.AddDays(-2).AddHours(13), "Gentle meal");

        // Day 7
        AddSeed(userId, "Extra Cheese Pepperoni Pizza", "Fast Food", ["cheese", "dairy", "gluten", "pork"], 5, now.AddDays(-1).AddHours(14), "Cheat meal");
    }

    private void AddSeed(Guid userId, string name, string cat, string[] ingredients, int rating, DateTime time, string notes)
    {
        var result = FoodEntry.Create(userId, name, cat, ingredients, rating, time, notes);
        if (result.IsSuccess)
        {
            _entries.Add(result.Value);
        }
    }

    public Task<IReadOnlyList<FoodEntry>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var list = _entries.Where(e => e.UserId == userId).OrderByDescending(e => e.ConsumedAtUtc).ToList();
            return Task.FromResult<IReadOnlyList<FoodEntry>>(list);
        }
    }

    public Task<FoodEntry?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var entry = _entries.FirstOrDefault(e => e.Id == id && e.UserId == userId);
            return Task.FromResult(entry);
        }
    }

    public Task AddAsync(FoodEntry entry, CancellationToken ct = default)
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
