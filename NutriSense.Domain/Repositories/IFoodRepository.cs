using NutriSense.Domain.Entities;

namespace NutriSense.Domain.Repositories;

public interface IFoodRepository
{
    Task<IReadOnlyList<FoodEntry>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<FoodEntry?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task AddAsync(FoodEntry entry, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
}
