using Cqrs.BackgroundServices;

namespace Cqrs.Outbox;

public interface IOutboxDispatcher
{
    Task ExecuteAsync(CancellationToken cancellationToken);
};
