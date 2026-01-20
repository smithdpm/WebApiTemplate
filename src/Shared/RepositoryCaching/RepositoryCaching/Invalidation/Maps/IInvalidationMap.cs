
namespace RepositoryCaching.Invalidation.Maps;
public interface IInvalidationMap
{
    IEnumerable<string> GetCacheKeysToInvalidate(ChangedEntity changedEntity);
}
