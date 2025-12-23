
namespace Application.Behaviours.RepositoryCaching;

public static class RepositoryCachingHelper
{
    public static string GenerateCacheKey<TId>(string entityName, TId id) where TId : notnull
    {
        return $"{entityName}-{id}";
    }
}

