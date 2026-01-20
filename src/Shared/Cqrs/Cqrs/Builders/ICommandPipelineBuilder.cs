using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Builders;

public interface ICommandPipelineBuilder
{
    IServiceCollection Services { get; }
    ICommandPipelineBuilder AddCustomDecoratorForCommandWithResult(Type decoratorType);
    ICommandPipelineBuilder AddCustomDecoratorCommandWithoutResult(Type decoratorType);
}

