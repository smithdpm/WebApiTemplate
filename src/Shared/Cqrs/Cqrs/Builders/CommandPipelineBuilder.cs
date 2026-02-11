
using Microsoft.Extensions.DependencyInjection;
using Cqrs.Messaging;
using Cqrs.Events.IntegrationEvents;
using Cqrs.Events.DomainEvents;
using Cqrs.Decorators.AtomicTransactionDecorator;
using Cqrs.Decorators.IntegrationEventToOutboxDecorator;
using Cqrs.Decorators.LoggingDecorator;
using Cqrs.Decorators.ValidationDecorator;

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
        Services.TryDecorate(typeof(ICommandHandler<,>), typeof(IntegrationEventToOutboxCommandDecorator<,>));
        Services.TryDecorate(typeof(ICommandHandler<>), typeof(IntegrationEventToOutboxCommandDecorator<>));
        Services.TryDecorate(typeof(IIntegrationEventHandler<>), typeof(IntegrationEventToOutboxIntegrationEventDecorator<>));
        Services.TryDecorate(typeof(IDomainEventHandler<>), typeof(IntegrationEventToOutboxDomainEventDecorator<>));
        return this;
    }

    internal ICommandPipelineBuilder AddAtomicTransactionHandling()
    {
        Services.TryDecorate(typeof(ICommandHandler<,>), typeof(AtomicTransactionCommandDecorator<,>));
        Services.TryDecorate(typeof(ICommandHandler<>), typeof(AtomicTransactionCommandDecorator<>));
        Services.TryDecorate(typeof(IIntegrationEventHandler<>), typeof(AtomicTransactionIntegrationEventDecorator<>));
        Services.TryDecorate(typeof(IDomainEventHandler<>), typeof(AtomicTransactionDomainEventDecorator<>));
        return this;
    }

    internal ICommandPipelineBuilder AddLogging()
    {
        Services.TryDecorate(typeof(ICommandHandler<,>), typeof(LoggingCommandDecorator<,>));
        Services.TryDecorate(typeof(ICommandHandler<>), typeof(LoggingCommandDecorator<>));
        Services.TryDecorate(typeof(IIntegrationEventHandler<>), typeof(LoggingIntegrationEventDecorator<>));
        Services.TryDecorate(typeof(IDomainEventHandler<>), typeof(LoggingDomainEventDecorator<>));
        return this;
    }

    internal ICommandPipelineBuilder AddValidation()
    {
        Services.TryDecorate(typeof(ICommandHandler<,>), typeof(ValidationCommandDecorator<,>));
        Services.TryDecorate(typeof(ICommandHandler<>), typeof(ValidationCommandDecorator<>));
        return this;
    }
}
