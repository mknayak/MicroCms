using MicroCMS.Domain.Entities;
using MicroCMS.Domain.Specifications;
using Microsoft.EntityFrameworkCore;

namespace MicroCMS.Infrastructure.Persistence.Common;

/// <summary>
/// Translates a domain <see cref="ISpecification{T}"/> into an EF Core LINQ query.
/// Applies criteria, eager-loading includes, ordering, and paging in a single pipeline.
///
/// Complexity kept low by processing each concern in its own dedicated static method.
/// All queries remain parameterised (no string interpolation).
/// </summary>
internal static class SpecificationEvaluator
{
    /// <summary>
    /// Builds an <see cref="IQueryable{T}"/> from the given specification.
    /// </summary>
    /// <param name="inputQuery">Base queryable (e.g. from DbSet).</param>
    /// <param name="specification">Specification to apply.</param>
    /// <param name="asNoTracking">
    /// When <c>true</c>, uses <see cref="EntityFrameworkQueryableExtensions.AsNoTracking{T}"/>
    /// (suitable for read-only queries).
    /// </param>
    public static IQueryable<T> GetQuery<T>(
        IQueryable<T> inputQuery,
        ISpecification<T> specification,
        bool asNoTracking = false)
        where T : class
    {
        var query = ApplyCriteria(inputQuery, specification);
        query = ApplyIncludes(query, specification);
        query = ApplyOrdering(query, specification);
        query = ApplyPaging(query, specification);

        return asNoTracking ? query.AsNoTracking() : query;
    }

    // ── Private pipeline steps ────────────────────────────────────────────

    private static IQueryable<T> ApplyCriteria<T>(
        IQueryable<T> query,
        ISpecification<T> specification)
        where T : class =>
        query.Where(specification.Criteria);

    private static IQueryable<T> ApplyIncludes<T>(
        IQueryable<T> query,
        ISpecification<T> specification)
        where T : class =>
        specification.Includes.Aggregate(query, (q, include) => q.Include(include));

    private static IQueryable<T> ApplyOrdering<T>(
        IQueryable<T> query,
        ISpecification<T> specification)
        where T : class
    {
        if (specification.OrderBy is not null)
        {
            return query.OrderBy(specification.OrderBy);
        }

        if (specification.OrderByDescending is not null)
        {
            return query.OrderByDescending(specification.OrderByDescending);
        }

        return query;
    }

    private static IQueryable<T> ApplyPaging<T>(
        IQueryable<T> query,
        ISpecification<T> specification)
        where T : class
    {
        if (!specification.IsPagingEnabled)
        {
            return query;
        }

        return query.Skip(specification.Skip).Take(specification.Take);
    }
}
