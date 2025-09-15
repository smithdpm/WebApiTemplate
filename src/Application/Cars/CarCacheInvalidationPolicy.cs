
using Application.Behaviours.RepositoryCaching;
using Application.Cars.GetByMake;
using Domain.Cars;

namespace Application.Cars;
internal class CarCacheInvalidationPolicy : StandardCacheInvalidationPolicy<Car, Guid>
{
    protected override IEnumerable<Func<Car, string?>> CacheInvalidationFunctionsByEntity()
    {
        yield return (entity) => new GetCarDtosByMakeSpec(entity.Make).CacheKey;
    }
}
