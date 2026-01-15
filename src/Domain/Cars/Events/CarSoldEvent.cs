using SharedKernel.Events;

namespace Domain.Cars.Events;

public record CarSoldEvent(Guid CarId, DateTime SoldAt, decimal SoldPrice) : DomainEventBase;
