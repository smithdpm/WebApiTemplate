using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Builders;

public interface ICommandPipelineBuilder
{
    IServiceCollection Services { get; }
    ICommandPipelineBuilder AddLogging();
    ICommandPipelineBuilder AddValidation();
    ICommandPipelineBuilder AddIntegrationEventHandling();
    ICommandPipelineBuilder AddCustomDecoratorForCommandWithResult(Type decoratorType);
    ICommandPipelineBuilder AddCustomDecoratorCommandWithoutResult(Type decoratorType);
    ICommandPipelineBuilder AddAtomicTransactionHandling();
}

