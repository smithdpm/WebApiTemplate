
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Behaviours;
using SharedKernel.Messaging;

namespace Application.Behaviours;
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

    public ICommandPipelineBuilder AddIntegrationEventHandling()
    {
        Services.Decorate(typeof(ICommandHandler<,>), typeof(IntegrationEventDecorator<,>));
        Services.Decorate(typeof(ICommandHandler<>), typeof(IntegrationEventDecorator<>));
        return this;
    }

    public ICommandPipelineBuilder AddLogging()
    {
        Services.Decorate(typeof(ICommandHandler<,>), typeof(LoggingDecorator<,>));
        Services.Decorate(typeof(ICommandHandler<>), typeof(LoggingDecorator<>));
        return this;
    }

    public ICommandPipelineBuilder AddValidation()
    {
        Services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationDecorator<,>));
        Services.Decorate(typeof(ICommandHandler<>), typeof(ValidationDecorator<>));
        return this;
    }
}
