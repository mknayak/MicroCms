using System.Linq.Expressions;

namespace MicroCMS.Domain.Specifications;

/// <summary>
/// Specification pattern: encapsulates a query predicate and optional ordering / includes.
/// </summary>
public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }
    int Take { get; }
    int Skip { get; }
    bool IsPagingEnabled { get; }
}
