using MicroCMS.Domain.Entities;
using MicroCMS.Domain.Specifications;

namespace MicroCMS.Domain.Repositories;

/// <summary>
/// Generic repository interface for aggregate roots.
/// Concrete implementations live in <c>MicroCMS.Infrastructure</c>.
/// </summary>
public interface IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> ListAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}
