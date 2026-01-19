namespace RepositoryCaching.Helpers;

public static class RepositoryCachingHelper
{
    public static string GenerateCacheKey(string entityName, string id)
    {
        return $"{entityName}-{id}";
    }
}

