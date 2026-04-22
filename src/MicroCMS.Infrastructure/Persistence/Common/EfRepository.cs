using MicroCMS.Domain.Entities;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications;
using Microsoft.EntityFrameworkCore;

namespace MicroCMS.Infrastructure.Persistence.Common;

/// <summary>
/// Generic EF Core implementation of <see cref="IRepository{TEntity,TId}"/>.
///
/// All queries are automatically scoped to the current tenant via the global
/// <see cref="HasQueryFilter"/> set in <see cref="ApplicationDbContext"/>.
///
/// No raw SQL — only LINQ expressions and parameterised EF Core queries.
/// </summary>
internal sealed class EfRepository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    private readonly ApplicationDbContext _context;

    public EfRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // ── Read operations ───────────────────────────────────────────────────

    public async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default) =>
        await _context.Set<TEntity>().FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<TEntity>> ListAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.GetQuery(
            _context.Set<TEntity>().AsQueryable(),
            specification,
            asNoTracking: false);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.GetQuery(
            _context.Set<TEntity>().AsQueryable(),
            specification,
            asNoTracking: true);

        return await query.CountAsync(cancellationToken);
    }

    // ── Write operations ──────────────────────────────────────────────────

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await _context.Set<TEntity>().AddAsync(entity, cancellationToken);

    public void Update(TEntity entity) =>
        _context.Set<TEntity>().Update(entity);

    public void Remove(TEntity entity) =>
        _context.Set<TEntity>().Remove(entity);
}
