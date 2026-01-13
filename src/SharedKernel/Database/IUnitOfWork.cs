

namespace Infrastructure.Database;
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    bool HasChanges();
}
