using MediatR;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Common.Markers;

namespace MicroCMS.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that calls <see cref="IUnitOfWork.SaveChangesAsync"/> after a
/// command handler completes successfully.
///
/// Only applies to types that implement <see cref="ICommand{TResponse}"/> or <see cref="ICommand"/>;
/// read-only queries are skipped to avoid unnecessary round-trips.
///
/// Pipeline position: last before the handler returns (inner-most wrapper).
/// The outbox interceptor, domain events, and any transactional changes are all flushed here
/// in a single <c>SaveChanges</c> call.
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        if (IsCommand())
        {
            // Use CancellationToken.None so a client disconnect (which cancels the HTTP
            // request token) never aborts a save that the handler already completed.
            // The handler ran successfully — the write must be committed regardless.
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }

        return response;
    }

    private static bool IsCommand() =>
        typeof(TRequest).GetInterfaces()
            .Any(i => i == typeof(ICommand) ||
                      (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>)));
}
