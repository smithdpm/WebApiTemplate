using Application.Abstractions.Services;
using Application.Behaviours.RepositoryCaching;
using Cqrs.Events.DomainEvents;
using Domain.Cars;
using Domain.Cars.Events;

namespace Application.Cars.SellCar;

public class CarCacheInvalidationHandler(ICacheService cacheService) : IDomainEventHandler<CarSoldEvent>
{
    public async Task HandleAsync(CarSoldEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var cacheKey = RepositoryCachingHelper.GenerateCacheKey(typeof(Car).Name, domainEvent.CarId.ToString());
        await cacheService.RemoveAsync(cacheKey, cancellationToken);
    }
}

