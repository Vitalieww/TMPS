using NutriSense.Domain.Entities;

namespace NutriSense.Domain.Repositories;

public interface IReactionRepository
{
    Task<IReadOnlyList<ReactionEntry>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<ReactionEntry?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task AddAsync(ReactionEntry entry, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
}
