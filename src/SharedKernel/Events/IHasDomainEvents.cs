
namespace SharedKernel.Events;

public interface IHasDomainEvents
{
    public IReadOnlyCollection<DomainEventBase> DomainEvents { get; }

    void AddDomainEvent(DomainEventBase eventItem);

    void ClearDomainEvents();
}

