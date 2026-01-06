
using Ardalis.Specification.EntityFrameworkCore;
using SharedKernel.Database;

namespace Infrastructure.Database;
public class EfRepository<T> : RepositoryBase<T>, IReadRepository<T>, IRepository<T> where T : class, IAggregateRoot
{
    protected readonly ApplicationContext _dbContext;
    
    public EfRepository(ApplicationContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

}
