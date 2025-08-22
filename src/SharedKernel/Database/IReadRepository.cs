using Ardalis.Specification;

namespace SharedKernel.Database;
public interface IReadRepository<T>: IReadRepositoryBase<T>
    where T : class, IAggregateRoot
{
    Task<TResult?> ProjectToFirstOrDefaultAsync<TResult>(ISpecification<T> specification, CancellationToken cancellationToken);
    Task<List<TResult>> ProjectToListAsync<TResult>(ISpecification<T> specification, CancellationToken cancellationToken);
}
