
using Ardalis.Specification;

namespace Application.Abstractions.Database;
public interface IRepository<T>: IRepositoryBase<T> where T : class, IAggregateRoot

{
    
}

