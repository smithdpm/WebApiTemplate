using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Application.Abstractions.Database;

namespace Infrastructure.Database;
internal sealed class EfRepository<T>: RepositoryBase<T>, IReadRepositoryBase<T>, IRepository<T> where T: class, IAggregateRoot
{
    public EfRepository(CatalogContext dbContext): base(dbContext)
    {
    }
}
