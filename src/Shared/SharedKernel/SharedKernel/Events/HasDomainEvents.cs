using System.ComponentModel.DataAnnotations.Schema;

namespace SharedKernel.Events;

public abstract class HasDomainEvents : IHasDomainEvents
{
    [NotMapped]
    public IReadOnlyCollection<DomainEventBase> DomainEvents => _domainEvents.AsReadOnly();
    private List<DomainEventBase> _domainEvents = new();

    public void AddDomainEvent(DomainEventBase eventItem) => _domainEvents.Add(eventItem);

    public void ClearDomainEvents() => _domainEvents.Clear();
    
}
