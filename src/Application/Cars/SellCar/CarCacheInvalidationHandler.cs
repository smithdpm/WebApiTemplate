using Cqrs.Events.DomainEvents;
using Domain.Cars;
using Domain.Cars.Events;
using RepositoryCaching.Cache;
using RepositoryCaching.Helpers;

namespace Application.Cars.SellCar;

public class CarCacheInvalidationHandler(ICacheService cacheService) : DomainEventHandler<CarSoldEvent>
{
    public override async Task HandleAsync(CarSoldEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var cacheKey = RepositoryCachingHelper.GenerateCacheKey(typeof(Car).Name, domainEvent.CarId.ToString());
        await cacheService.RemoveAsync(cacheKey, cancellationToken);
    }
}

