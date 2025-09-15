using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Behaviours.RepositoryCaching;

public static class RepositoryCachingHelper
{
    public static string GenerateCacheKey<TId>(string entityName, TId id) where TId : notnull
    {
        return $"{entityName}-{id}";
    }
}

