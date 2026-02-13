using Cqrs.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace Cqrs.BackgroundServices;

[EditorBrowsable(EditorBrowsableState.Never)]
public class OutboxDispatcherService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxDispatcherService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();

                var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>();

                await dispatcher.ExecuteAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing Outbox");
            }
        }        
    }
}