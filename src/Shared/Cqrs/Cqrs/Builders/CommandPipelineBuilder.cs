
using Microsoft.Extensions.DependencyInjection;
using Cqrs.Decorators;
using Cqrs.Messaging;

namespace Cqrs.Builders;
internal class CommandPipelineBuilder(IServiceCollection services) : ICommandPipelineBuilder
{
    public IServiceCollection Services => services;

    public ICommandPipelineBuilder AddCustomDecoratorCommandWithoutResult(Type decoratorType)
    {

        Services.TryDecorate(typeof(ICommandHandler<>), decoratorType);
        return this;
    }

    public ICommandPipelineBuilder AddCustomDecoratorForCommandWithResult(Type decoratorType)
    {
        Services.TryDecorate(typeof(ICommandHandler<,>), decoratorType);
        return this;
    }

    internal ICommandPipelineBuilder AddIntegrationEventHandling()
    {
        Services.TryDecorate(typeof(ICommandHandler<,>), typeof(IntegrationEventDecorator<,>));
        Services.TryDecorate(typeof(ICommandHandler<>), typeof(IntegrationEventDecorator<>));
        return this;
    }

    internal ICommandPipelineBuilder AddAtomicTransactionHandling()
    {
        Services.TryDecorate(typeof(ICommandHandler<,>), typeof(AtomicTransactionDecorator<,>));
        Services.TryDecorate(typeof(ICommandHandler<>), typeof(AtomicTransactionDecorator<>));
        return this;
    }

    internal ICommandPipelineBuilder AddLogging()
    {
        Services.TryDecorate(typeof(ICommandHandler<,>), typeof(LoggingDecorator<,>));
        Services.TryDecorate(typeof(ICommandHandler<>), typeof(LoggingDecorator<>));
        return this;
    }

    internal ICommandPipelineBuilder AddValidation()
    {
        Services.TryDecorate(typeof(ICommandHandler<,>), typeof(ValidationDecorator<,>));
        Services.TryDecorate(typeof(ICommandHandler<>), typeof(ValidationDecorator<>));
        return this;
    }
}
