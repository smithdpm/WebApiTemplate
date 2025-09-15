using Application.Abstractions.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Database
{
    public class MemoryCache : ICacheService
    {
        private readonly IMemoryCache _memoryCache;

        public MemoryCache(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_memoryCache.Get<T>(key));
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _memoryCache.Remove(key);
            return Task.CompletedTask;
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default)
        {
            if (absoluteExpiration != null)
            {
                _memoryCache.Set(key, value, (TimeSpan)absoluteExpiration);
            }
            else
            {
                _memoryCache.Set(key, value);
            }

            return Task.CompletedTask;
        }
    }
}
