

using Microsoft.Extensions.DependencyInjection;

namespace SharedKernel.Behaviours;

public interface ICommandPipelineBuilder
{
    IServiceCollection Services { get; }
    ICommandPipelineBuilder AddLogging();
    ICommandPipelineBuilder AddValidation();
    ICommandPipelineBuilder AddIntegrationEventHandling();
    ICommandPipelineBuilder AddCustomDecoratorForCommandWithResult(Type decoratorType);
    ICommandPipelineBuilder AddCustomDecoratorCommandWithoutResult(Type decoratorType);
}

