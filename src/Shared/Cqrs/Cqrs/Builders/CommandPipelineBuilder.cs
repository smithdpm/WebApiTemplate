
using Microsoft.Extensions.DependencyInjection;
using Cqrs.Decorators;
using Cqrs.Messaging;

namespace Cqrs.Builders;
internal class CommandPipelineBuilder(IServiceCollection services) : ICommandPipelineBuilder
{
    public IServiceCollection Services => services;

    public ICommandPipelineBuilder AddCustomDecoratorCommandWithoutResult(Type decoratorType)
    {
        Services.Decorate(typeof(ICommandHandler<>), decoratorType);
        return this;
    }

    public ICommandPipelineBuilder AddCustomDecoratorForCommandWithResult(Type decoratorType)
    {
        Services.Decorate(typeof(ICommandHandler<,>), decoratorType);
        return this;
    }

    internal ICommandPipelineBuilder AddIntegrationEventHandling()
    {
        Services.Decorate(typeof(ICommandHandler<,>), typeof(IntegrationEventDecorator<,>));
        Services.Decorate(typeof(ICommandHandler<>), typeof(IntegrationEventDecorator<>));
        return this;
    }

    internal ICommandPipelineBuilder AddAtomicTransactionHandling()
    {
        Services.Decorate(typeof(ICommandHandler<,>), typeof(AtomicTransactionDecorator<,>));
        Services.Decorate(typeof(ICommandHandler<>), typeof(AtomicTransactionDecorator<>));
        return this;
    }

    internal ICommandPipelineBuilder AddLogging()
    {
        Services.Decorate(typeof(ICommandHandler<,>), typeof(LoggingDecorator<,>));
        Services.Decorate(typeof(ICommandHandler<>), typeof(LoggingDecorator<>));
        return this;
    }

    internal ICommandPipelineBuilder AddValidation()
    {
        Services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationDecorator<,>));
        Services.Decorate(typeof(ICommandHandler<>), typeof(ValidationDecorator<>));
        return this;
    }
}
