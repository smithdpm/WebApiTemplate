using SharedKernel.Database;
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Database;
public sealed class EfRepository<T> : RepositoryBase<T>, IReadRepository<T>, IRepository<T> where T : class, IAggregateRoot
{
    public EfRepository(CatalogContext dbContext) : base(dbContext)
    {
    }

    public async Task<TResult?> ProjectToFirstOrDefaultAsync<TResult>(ISpecification<T> specification, CancellationToken cancellationToken)
    {
        return await ApplySpecification(specification)
            .AsNoTracking()
            .ProjectToType<TResult>()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<TResult>> ProjectToListAsync<TResult>(ISpecification<T> specification, CancellationToken cancellationToken)
    {
        return ApplySpecification(specification)
            .AsNoTracking()
            .ProjectToType<TResult>()
            .ToListAsync(cancellationToken);
    }
}
