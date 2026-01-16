
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Database;
public class EfUnitOfWork<TDbContext>(TDbContext dbContext) : IUnitOfWork
    where TDbContext : DbContext
{
    public bool HasChanges() => dbContext.ChangeTracker.HasChanges();


    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await dbContext.SaveChangesAsync(cancellationToken);

}
