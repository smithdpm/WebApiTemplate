using Application.Abstractions.Events;
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Mapster;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Database;
using SharedKernel.Events;

namespace Infrastructure.Database;
public class EfRepository<T> : RepositoryBase<T>, IReadRepository<T>, IRepository<T> where T : class, IAggregateRoot
{
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    protected readonly CatalogContext _dbContext;
    
    public EfRepository(CatalogContext dbContext
        , IDomainEventDispatcher domainEventDispatcher) : base(dbContext)
    {
        _dbContext = dbContext;
        _domainEventDispatcher = domainEventDispatcher;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = GetDomainEvents();

        var result = await base.SaveChangesAsync(cancellationToken);

        await _domainEventDispatcher.DispatchEventsAsync(domainEvents, cancellationToken);

        return result;
    }

    private List<DomainEventBase> GetDomainEvents()
    {
        return _dbContext.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(entity => entity.Entity)
            .SelectMany(entity =>
            {
                var events = entity.DomainEvents.ToList();
                entity.ClearDomainEvents();
                return events;
            }).ToList();
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
