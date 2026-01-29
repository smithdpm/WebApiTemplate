
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Database;

namespace Shop.Application.Database;

public class AtomicRepositoryBase<T>: RepositoryBase<T>, IRepository<T>
    where T : class, IAggregateRoot
{
    public AtomicRepositoryBase(ApplicationDbContext dbContext)
        : this(dbContext, Ardalis.Specification.EntityFrameworkCore.SpecificationEvaluator.Default)
    {
    }

    public AtomicRepositoryBase(ApplicationDbContext dbContext, ISpecificationEvaluator specificationEvaluator)
        : base(dbContext, specificationEvaluator)
    {
    }

    public override async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbContext.Set<T>().Add(entity);

        return entity;
    }

    /// <inheritdoc/>
    public override async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        DbContext.Set<T>().AddRange(entities);

        return entities;
    }

    /// <inheritdoc/>
    public override async Task<int> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbContext.Set<T>().Update(entity);

        return 1;
    }

    /// <inheritdoc/>
    public override async Task<int> UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        DbContext.Set<T>().UpdateRange(entities);

        return entities.Count();
    }

    /// <inheritdoc/>
    public override async Task<int> DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbContext.Set<T>().Remove(entity);

        return 1;
    }

    /// <inheritdoc/>
    public override async Task<int> DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        DbContext.Set<T>().RemoveRange(entities);
        return entities.Count();
    }

    /// <inheritdoc/>
    public override async Task<int> DeleteRangeAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification);
        DbContext.Set<T>().RemoveRange(query);
        return query.Count();
    }

}
