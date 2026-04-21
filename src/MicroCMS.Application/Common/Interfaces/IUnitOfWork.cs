namespace MicroCMS.Application.Common.Interfaces;

/// <summary>
/// Unit of Work abstraction. Implementations (EF Core, etc.) live in Infrastructure.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
