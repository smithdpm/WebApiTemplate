
using SharedKernel.Events;

namespace Cqrs.DomainTestFixture;

public record ItemNameUpdatedDomainEvent (
    Guid ItemId,
    string OldName,
    string NewName
): DomainEventBase;
