
using Ardalis.Specification;

namespace SharedKernel.Database;
public interface IRepository<T>: IRepositoryBase<T> where T : class, IAggregateRoot;
