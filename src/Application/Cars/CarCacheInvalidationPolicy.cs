using Application.Cars.GetByMake;
using Domain.Cars;
using RepositoryCaching.Invalidation.Policies;

namespace Application.Cars;
internal class CarCacheInvalidationPolicy : StandardCacheInvalidationPolicy<Car>
{
    protected override IEnumerable<Func<Car, string?>> CacheInvalidationFunctionsByEntity()
    {
        yield return (entity) => new GetCarDtosByMakeSpec(entity.Make).CacheKey;
    }
}
