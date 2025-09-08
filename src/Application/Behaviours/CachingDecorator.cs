
using Application.Services;
using Ardalis.Specification;
using SharedKernel.Database;

namespace Application.Behaviours;

public static class CachingDecorator
{
    public sealed class CachedRepository<T>(
        IRepository<T> inner,
        ICacheService cacheService) : IRepository<T>
        where T : class, IAggregateRoot
    {
        public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            return await inner.AddAsync(entity, cancellationToken);
        }

        public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            return await inner.AddRangeAsync(entities, cancellationToken);
        }

        public async Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            return await inner.AnyAsync(specification, cancellationToken);
        }

        public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        {
            return await inner.AnyAsync(cancellationToken);
        }

        public IAsyncEnumerable<T> AsAsyncEnumerable(ISpecification<T> specification)
        {
            return inner.AsAsyncEnumerable(specification);
        }

        public async Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            return await inner.CountAsync(specification, cancellationToken);
        }

        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            return await inner.CountAsync(cancellationToken);
        }

        public async Task<int> DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            return await inner.DeleteAsync(entity, cancellationToken);
        }

        public async Task<int> DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            return await inner.DeleteRangeAsync(entities, cancellationToken);
        }

        public async Task<int> DeleteRangeAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            return await inner.DeleteRangeAsync(specification, cancellationToken);
        }

        public async Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            if (!specification.CacheEnabled)
                return await inner.FirstOrDefaultAsync(specification, cancellationToken);

            var cachedResults = await cacheService.GetAsync<List<T>>(specification.CacheKey, cancellationToken);
            if (cachedResults != null && cachedResults.Any())
                return cachedResults.First();

            var results = await inner.ListAsync(specification, cancellationToken);

            if (results != null)
                await cacheService.SetAsync(specification.CacheKey, results, TimeSpan.FromMinutes(5), cancellationToken);

            return results.FirstOrDefault();
        }

        public async Task<T?> FirstOrDefaultAsync(ISingleResultSpecification<T> specification, CancellationToken cancellationToken = default)
        {
            if (!specification.CacheEnabled)
                return await inner.FirstOrDefaultAsync(specification, cancellationToken);

            var cachedResult = await cacheService.GetAsync<T>(specification.CacheKey, cancellationToken);
            if (cachedResult != null)
                return cachedResult;

            var result = await inner.FirstOrDefaultAsync(specification, cancellationToken);

            if (result != null)
                await cacheService.SetAsync(specification.CacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);

            return result;
        }

        public async Task<TResult?> FirstOrDefaultAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        {
            if (!specification.CacheEnabled)
                return await inner.FirstOrDefaultAsync<TResult>(specification, cancellationToken);  

            var cachedResults = await cacheService.GetAsync<List<TResult>>(specification.CacheKey, cancellationToken);
            
            if (cachedResults != null && cachedResults.Any())
                return cachedResults.First();

            var results = await inner.FirstOrDefaultAsync(specification, cancellationToken);

            if (results != null)
                await cacheService.SetAsync(specification.CacheKey, results, TimeSpan.FromMinutes(5), cancellationToken);

            return results;
        }

        public async Task<TResult?> FirstOrDefaultAsync<TResult>(ISingleResultSpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        {
            if (!specification.CacheEnabled)
                return await inner.FirstOrDefaultAsync<TResult>(specification, cancellationToken);

            var cachedResult = await cacheService.GetAsync<TResult>(specification.CacheKey, cancellationToken);

            if (cachedResult != null)
                return cachedResult;

            var result = await inner.FirstOrDefaultAsync(specification, cancellationToken);

            if (result != null)
                await cacheService.SetAsync(specification.CacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);

            return result;
        }

        public async Task<T?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull
        {
            string cacheKey = GenerateCacheKey(id);
            var cachedEntity = await cacheService.GetAsync<T>(cacheKey, cancellationToken);
            if (cachedEntity != null)
                return cachedEntity;

            var entity = await inner.GetByIdAsync(id, cancellationToken);

            if (entity != null)
                await cacheService.SetAsync(cacheKey, entity, TimeSpan.FromMinutes(5), cancellationToken);

            return entity;
        }

        public async Task<List<T>> ListAsync(CancellationToken cancellationToken = default)
        {
            return await inner.ListAsync(cancellationToken);
        }

        public async Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            if (!specification.CacheEnabled)
                return await inner.ListAsync(specification, cancellationToken);

            var cachedResults = await cacheService.GetAsync<List<T>>(specification.CacheKey, cancellationToken);

            if (cachedResults != null && cachedResults.Any())
                return cachedResults;

            var results = await inner.ListAsync(specification, cancellationToken);

            if (results != null)
                await cacheService.SetAsync(specification.CacheKey, results, TimeSpan.FromMinutes(5), cancellationToken);

            return results;
        }

        public async Task<List<TResult>> ListAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        {
            if (!specification.CacheEnabled)
                return await inner.ListAsync<TResult>(specification, cancellationToken);

            var cachedResults = await cacheService.GetAsync<List<TResult>>(specification.CacheKey, cancellationToken);

            if (cachedResults != null && cachedResults.Any())
                return cachedResults;

            var results = await inner.ListAsync<TResult>(specification, cancellationToken);

            if (results != null)
                await cacheService.SetAsync(specification.CacheKey, results, TimeSpan.FromMinutes(5), cancellationToken);

            return results;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await inner.SaveChangesAsync(cancellationToken);
        }

        public async Task<T?> SingleOrDefaultAsync(ISingleResultSpecification<T> specification, CancellationToken cancellationToken = default)
        {
            if (!specification.CacheEnabled)
                return await inner.SingleOrDefaultAsync(specification, cancellationToken);

            var cachedResult = await cacheService.GetAsync<T>(specification.CacheKey, cancellationToken);

            if (cachedResult != null)
                return cachedResult;

            var result = await inner.SingleOrDefaultAsync(specification, cancellationToken);

            if (result != null)
                await cacheService.SetAsync(specification.CacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);

            return result;
        }

        public async Task<TResult?> SingleOrDefaultAsync<TResult>(ISingleResultSpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        {
            if (!specification.CacheEnabled)
                return await inner.SingleOrDefaultAsync<TResult>(specification, cancellationToken);

            var cachedResult = await cacheService.GetAsync<TResult>(specification.CacheKey, cancellationToken);

            if (cachedResult != null)
                return cachedResult;

            var result = await inner.SingleOrDefaultAsync<TResult>(specification, cancellationToken);

            if (result != null)
                await cacheService.SetAsync(specification.CacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);

            return result;
        }

        public async Task<int> UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            return await inner.UpdateAsync(entity, cancellationToken);
        }

        public async Task<int> UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            return await inner.UpdateRangeAsync(entities, cancellationToken);
        }

        private string GenerateCacheKey<TId>(TId id) where TId : notnull
        {
            return $"{typeof(T).Name}-{id}";
        }

    }
}
