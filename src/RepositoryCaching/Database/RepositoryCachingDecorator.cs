using Ardalis.Specification;
using Microsoft.Extensions.Options;
using RepositoryCaching.Cache;
using RepositoryCaching.Configuration;
using RepositoryCaching.Helpers;
using SharedKernel.Database;

namespace RepositoryCaching.Database;

public static class RepositoryCachingDecorator
{
    public sealed class CachedRepository<T> : IRepository<T>, IReadRepository<T>
        where T : class, IAggregateRoot
    {
        private readonly RepositoryCacheSettings _cacheSettings;
        private readonly EntityCacheSettings _entityCacheSettings;
        private readonly IRepository<T> _inner;
        private readonly ICacheService _cacheService;

        public CachedRepository(IRepository<T> inner,
        ICacheService cacheService,
        IOptions<RepositoryCacheSettings> options)
        {
            _inner = inner;
            _cacheService = cacheService;
            _cacheSettings = options.Value;
            _entityCacheSettings = _cacheSettings.PerEntitySettings
                .FirstOrDefault(e => e.Key == typeof(T).Name).Value
                ?? new EntityCacheSettings();
        }
        public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            return await _inner.AddAsync(entity, cancellationToken);
        }

        public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            return await _inner.AddRangeAsync(entities, cancellationToken);
        }

        public async Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            return await _inner.AnyAsync(specification, cancellationToken);
        }

        public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        {
            return await _inner.AnyAsync(cancellationToken);
        }

        public IAsyncEnumerable<T> AsAsyncEnumerable(ISpecification<T> specification)
        {
            return _inner.AsAsyncEnumerable(specification);
        }

        public async Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            return await _inner.CountAsync(specification, cancellationToken);
        }

        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            return await _inner.CountAsync(cancellationToken);
        }

        public async Task<int> DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            return await _inner.DeleteAsync(entity, cancellationToken);
        }

        public async Task<int> DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            return await _inner.DeleteRangeAsync(entities, cancellationToken);
        }

        public async Task<int> DeleteRangeAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            return await _inner.DeleteRangeAsync(specification, cancellationToken);
        }

        public async Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            if (!IsCachingEnabled(specification))
                return await _inner.FirstOrDefaultAsync(specification, cancellationToken);

            var cachedResults = await _cacheService.GetAsync<List<T>>(specification.CacheKey!, cancellationToken);
            if (cachedResults != null && cachedResults.Any())
                return cachedResults.First();

            var results = await _inner.ListAsync(specification, cancellationToken);

            if (results != null)
                await _cacheService.SetAsync(specification.CacheKey!, results, TimeSpan.FromMinutes(GetExpiration()), cancellationToken);

            return results!.FirstOrDefault();
        }

        public async Task<T?> FirstOrDefaultAsync(ISingleResultSpecification<T> specification, CancellationToken cancellationToken = default)
        {
            if (!IsCachingEnabled(specification))
                return await _inner.FirstOrDefaultAsync(specification, cancellationToken);

            var cachedResult = await _cacheService.GetAsync<T>(specification.CacheKey!, cancellationToken);
            if (cachedResult != null)
                return cachedResult;

            var result = await _inner.FirstOrDefaultAsync(specification, cancellationToken);

            if (result != null)
                await _cacheService.SetAsync(specification.CacheKey!, result, TimeSpan.FromMinutes(GetExpiration()), cancellationToken);

            return result;
        }

        public async Task<TResult?> FirstOrDefaultAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        {
            if (!IsCachingEnabled(specification))
                return await _inner.FirstOrDefaultAsync(specification, cancellationToken);  

            var cachedResults = await _cacheService.GetAsync<List<TResult>>(specification.CacheKey!, cancellationToken);
            
            if (cachedResults != null && cachedResults.Any())
                return cachedResults.First();

            var results = await _inner.FirstOrDefaultAsync(specification, cancellationToken);

            if (results != null)
                await _cacheService.SetAsync(specification.CacheKey!, results, TimeSpan.FromMinutes(GetExpiration()), cancellationToken);

            return results;
        }

        public async Task<TResult?> FirstOrDefaultAsync<TResult>(ISingleResultSpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        {
            if (!IsCachingEnabled(specification))
                return await _inner.FirstOrDefaultAsync(specification, cancellationToken);

            var cachedResult = await _cacheService.GetAsync<TResult>(specification.CacheKey!, cancellationToken);

            if (cachedResult != null)
                return cachedResult;

            var result = await _inner.FirstOrDefaultAsync(specification, cancellationToken);

            if (result != null)
                await _cacheService.SetAsync(specification.CacheKey!, result, TimeSpan.FromMinutes(GetExpiration()), cancellationToken);

            return result;
        }

        public async Task<T?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull
        {
            if (!IsCachingEnabled())
                return await _inner.GetByIdAsync(id, cancellationToken);

            string cacheKey = RepositoryCachingHelper.GenerateCacheKey(typeof(T).Name, id.ToString()!);
            var cachedEntity = await _cacheService.GetAsync<T>(cacheKey, cancellationToken);
            if (cachedEntity != null)
                return cachedEntity;

            var entity = await _inner.GetByIdAsync(id, cancellationToken);

            if (entity != null)
                await _cacheService.SetAsync(cacheKey, entity, TimeSpan.FromMinutes(GetExpiration()), cancellationToken);

            return entity;
        }

        public async Task<List<T>> ListAsync(CancellationToken cancellationToken = default)
        {
            return await _inner.ListAsync(cancellationToken);
        }

        public async Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            if (!IsCachingEnabled(specification))
                return await _inner.ListAsync(specification, cancellationToken);

            var cachedResults = await _cacheService.GetAsync<List<T>>(specification.CacheKey!, cancellationToken);

            if (cachedResults != null)
                return cachedResults;

            var results = await _inner.ListAsync(specification, cancellationToken);

            if (results != null)
                await _cacheService.SetAsync(specification.CacheKey!, results, TimeSpan.FromMinutes(GetExpiration()), cancellationToken);

            return results;
        }

        public async Task<List<TResult>> ListAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        {
            if (!IsCachingEnabled(specification))
                return await _inner.ListAsync(specification, cancellationToken);

            var cachedResults = await _cacheService.GetAsync<List<TResult>>(specification.CacheKey!, cancellationToken);

            if (cachedResults != null)
                return cachedResults;

            var results = await _inner.ListAsync(specification, cancellationToken);

            if (results != null)
                await _cacheService.SetAsync(specification.CacheKey!, results, TimeSpan.FromMinutes(GetExpiration()), cancellationToken);

            return results;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _inner.SaveChangesAsync(cancellationToken);
        }

        public async Task<T?> SingleOrDefaultAsync(ISingleResultSpecification<T> specification, CancellationToken cancellationToken = default)
        {
            if (!IsCachingEnabled(specification))
                return await _inner.SingleOrDefaultAsync(specification, cancellationToken);

            var cachedResult = await _cacheService.GetAsync<T>(specification.CacheKey!, cancellationToken);

            if (cachedResult != null)
                return cachedResult;

            var result = await _inner.SingleOrDefaultAsync(specification, cancellationToken);

            if (result != null)
                await _cacheService.SetAsync(specification.CacheKey!, result, TimeSpan.FromMinutes(GetExpiration()), cancellationToken);

            return result;
        }

        public async Task<TResult?> SingleOrDefaultAsync<TResult>(ISingleResultSpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        {
            if (!IsCachingEnabled(specification))
                return await _inner.SingleOrDefaultAsync(specification, cancellationToken);

            var cachedResult = await _cacheService.GetAsync<TResult>(specification.CacheKey!, cancellationToken);

            if (cachedResult != null)
                return cachedResult;

            var result = await _inner.SingleOrDefaultAsync(specification, cancellationToken);

            if (result != null)
                await _cacheService.SetAsync(specification.CacheKey!, result, TimeSpan.FromMinutes(GetExpiration()), cancellationToken);

            return result;
        }

        public async Task<int> UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            return await _inner.UpdateAsync(entity, cancellationToken);
        }

        public async Task<int> UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            return await _inner.UpdateRangeAsync(entities, cancellationToken);
        }      

        private bool IsCachingEnabled()
        {
            return _cacheSettings.Enabled && _entityCacheSettings.Enabled;
        }

        private bool IsCachingEnabled(ISpecification<T> specification)
        {
            return IsCachingEnabled() && specification.CacheEnabled;
        }

        private bool IsCachingEnabled<TResult>(ISpecification<T, TResult> specification)
        {
            return IsCachingEnabled() && specification.CacheEnabled;
        }

        private int GetExpiration()
        {
            int expiration = _entityCacheSettings.ExpirationInMinutes ?? _cacheSettings.DefaultExpirationInMinutes;
            return expiration;
        }
    }
}
