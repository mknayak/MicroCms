using MediatR;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Common.Markers;

/// <summary>
/// Marker interface for read-only queries.
/// Queries are <em>not</em> wrapped by <see cref="../Behaviors/UnitOfWorkBehavior{TRequest,TResponse}"/>;
/// they never mutate state and therefore never need a <c>SaveChanges</c> call.
/// </summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
