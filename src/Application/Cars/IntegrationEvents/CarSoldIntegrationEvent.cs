using Cqrs.Events.IntegrationEvents;

namespace Application.Cars.IntegrationEvents;
public record CarSoldIntegrationEvent(Guid CarId, DateTime SoldAt, decimal SoldPrice) : IntegrationEventBase;
