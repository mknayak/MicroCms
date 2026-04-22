using MicroCMS.Application.Common.Interfaces;

namespace MicroCMS.Infrastructure.Persistence.Common;

/// <summary>
/// EF Core implementation of <see cref="IUnitOfWork"/>.
///
/// Delegates to <see cref="ApplicationDbContext.SaveChangesAsync"/>, which triggers the
/// <c>DomainEventsToOutboxInterceptor</c> interceptor. Domain events are therefore written
/// to the outbox table atomically within the same transaction as the aggregate changes.
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
