using System.Runtime.CompilerServices;
using Cqrs.ApplicationTestFixture;
using Cqrs.Builders;
using Cqrs.Decorators;
using Cqrs.Decorators.Registries;
using Cqrs.DomainTestFixture;
using Cqrs.Events.DomainEvents;
using Cqrs.Events.IntegrationEvents;
using Cqrs.Messaging;
using Cqrs.Outbox;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SharedKernel.Database;
using Shouldly;

namespace Cqrs.UnitTests.Builders;

public class CommandRegistrationExtensionTests
{
    [Fact]
    public void AddCqrsBehaviours_ShouldRegisterCommandHandlersAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var applicationAssembly = typeof(BasicApplicationClass).Assembly;
        var domainAssembly = typeof(BasicDomainClass).Assembly;

        services.AddFakeLogging();
        services.AddScoped(sp => Substitute.For<IRepository<OutboxMessage>>());
        services.AddScoped(sp => Substitute.For<IUnitOfWork>());

        // Act
        services.AddCqrsBehaviours(applicationAssembly, domainAssembly, builder => { });
        var serviceProvider = services.BuildServiceProvider(); 

        // Assert
        using var scope = serviceProvider.CreateScope();

        var handler = scope.ServiceProvider.GetService<ICommandHandler<ApplicationTestFixture.TestCommand, string>>();
        handler.ShouldNotBeNull();
    }

        [Fact]
    public void AddCqrsBehaviours_ShouldNotThrowError_WhenNoHandlersInApplicationAssembly()
    {
        // Arrange
        var services = new ServiceCollection();
        var applicationAssembly = typeof(BasicDomainClass).Assembly;
        var domainAssembly = typeof(BasicDomainClass).Assembly;

        // Act
        var exception = Record.Exception(() =>
        {
            services.AddCqrsBehaviours(applicationAssembly, domainAssembly, builder => { });
            var serviceProvider = services.BuildServiceProvider(); 
        });

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void AddCqrsBehaviours_ShouldRegisterQueryHandlersAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var applicationAssembly = typeof(BasicApplicationClass).Assembly;
        var domainAssembly = typeof(BasicDomainClass).Assembly;

        // Act
        services.AddCqrsBehaviours(applicationAssembly, domainAssembly, builder => { });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        
        var handler = scope.ServiceProvider.GetService<IQueryHandler<ApplicationTestFixture.TestQuery, string>>();
        handler.ShouldNotBeNull();
    }

    [Fact]
    public void AddCqrsBehaviours_ShouldAddStandardDecoratorsInCorrectOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        var applicationAssembly = typeof(BasicApplicationClass).Assembly;
        var domainAssembly = typeof(BasicDomainClass).Assembly;

        services.AddFakeLogging();
        services.AddScoped(sp => Substitute.For<IRepository<OutboxMessage>>());
        services.AddScoped(sp => Substitute.For<IUnitOfWork>());

        // Act
        services.AddCqrsBehaviours(applicationAssembly, domainAssembly, builder => { });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        
        var handler1 = scope.ServiceProvider.GetRequiredService<ICommandHandler<ApplicationTestFixture.TestCommand, string>>();
        handler1.ShouldBeOfType<LoggingDecorator<ApplicationTestFixture.TestCommand, string>>();
        var handler2 = HandlerTestingExtensions<TestCommand, string>.GetInnerHandler((LoggingDecorator<ApplicationTestFixture.TestCommand, string>)handler1);
        handler2.ShouldBeOfType<ValidationDecorator<TestCommand, string>>();
        var handler3 = HandlerTestingExtensions<TestCommand, string>.GetInnerHandler((ValidationDecorator<TestCommand, string>)handler2);
        handler3.ShouldBeOfType<AtomicTransactionDecorator<TestCommand, string>>();
        var handler4 = HandlerTestingExtensions<TestCommand, string>.GetInnerHandler((  AtomicTransactionDecorator<TestCommand, string>)handler3);
        handler4.ShouldBeOfType<IntegrationEventDecorator<TestCommand, string>>();
        var handler5 = HandlerTestingExtensions<TestCommand, string>.GetInnerHandler((IntegrationEventDecorator<TestCommand, string>)handler4);
        handler5.ShouldBeOfType<ApplicationTestFixture.TestCommandHandler>();
    }

    [Fact]
    public void AddCqrsBehaviours_ShouldAddDomainEvents_WhenPresentInDomainAssembly()
    {
        // Arrange
        var services = new ServiceCollection();
        var applicationAssembly = typeof(BasicApplicationClass).Assembly;
        var domainAssembly = typeof(BasicDomainClass).Assembly;

        // Act
        services.AddCqrsBehaviours(applicationAssembly, domainAssembly, builder => { });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var eventTypeRegistry = serviceProvider.GetRequiredService<IEventTypeRegistry>();
        
        var eventType = eventTypeRegistry.GetTypeByName(nameof(DomainTestFixture.TestDomainEvent));
        eventType.ShouldBe(typeof(DomainTestFixture.TestDomainEvent));
    }

    [Fact]
    public void AddCqrsBehaviours_ShouldNotAddDomainEvents_WhenPresentApplicationAssembly()
    {
        // Arrange
        var services = new ServiceCollection();
        var applicationAssemblyWithDomainEvents = typeof(BasicDomainClass).Assembly;
        var domainAssemblyWithNoDomainEvents = typeof(BasicApplicationClass).Assembly;

        // Act
        services.AddCqrsBehaviours(applicationAssemblyWithDomainEvents, domainAssemblyWithNoDomainEvents, builder => { });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var eventTypeRegistry = serviceProvider.GetRequiredService<IEventTypeRegistry>();
        
        var eventType = eventTypeRegistry.GetTypeByName(nameof(DomainTestFixture.TestDomainEvent));
        eventType.ShouldBeNull();
    }

    [Fact]
    public void AddCqrsBehaviours_ShouldAddIntegrationEvents_WhenPresentInApplicationAssembly()
    {
        // Arrange
        var services = new ServiceCollection();
        var applicationAssembly = typeof(BasicApplicationClass).Assembly;
        var domainAssembly = typeof(BasicDomainClass).Assembly;

        // Act
        services.AddCqrsBehaviours(applicationAssembly, domainAssembly, builder => { });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var eventTypeRegistry = serviceProvider.GetRequiredService<IEventTypeRegistry>();
        
        var eventType = eventTypeRegistry.GetTypeByName(nameof(ApplicationTestFixture.TestIntegrationEvent));
        eventType.ShouldBe(typeof(ApplicationTestFixture.TestIntegrationEvent));
    }

    [Fact]
    public void AddCqrsBehaviours_ShouldNotAddIntegrationEvents_WhenPresentDomainAssembly()
    {
        // Arrange
        var services = new ServiceCollection();
        var applicationAssemblyWithNoIntegrationEvents = typeof(BasicDomainClass).Assembly;
        var domainAssemblyWithIntegrationEvents = typeof(BasicApplicationClass).Assembly;

        // Act
        services.AddCqrsBehaviours(applicationAssemblyWithNoIntegrationEvents, domainAssemblyWithIntegrationEvents, builder => { });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var eventTypeRegistry = serviceProvider.GetRequiredService<IEventTypeRegistry>();
        
        var eventType = eventTypeRegistry.GetTypeByName(nameof(ApplicationTestFixture.TestIntegrationEvent));
        eventType.ShouldBeNull();
    }

    [Fact]
    public void AddCqrsBehaviours_ShouldNotAddIntegrationEventHandler_WhenPresentDomainAssembly()
    {
        // Arrange
        var services = new ServiceCollection();
        var applicationAssemblyWithoutIntegrationEventHandlers = typeof(BasicDomainClass).Assembly;
        var domainAssemblyWithIntegrationEventHandlers = typeof(BasicApplicationClass).Assembly;

        // Act
        services.AddCqrsBehaviours(applicationAssemblyWithoutIntegrationEventHandlers, domainAssemblyWithIntegrationEventHandlers, builder => { });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var integrationEventHandler = serviceProvider.GetService<IIntegrationEventHandler<ApplicationTestFixture.TestIntegrationEvent>>();
        
        integrationEventHandler.ShouldBeNull();
    }

    [Fact]
    public void AddCqrsBehaviours_ShouldAddIntegrationEventHandler_WhenPresentApplicationAssembly()
    {
        // Arrange
        var services = new ServiceCollection();
        var applicationAssembly = typeof(BasicApplicationClass).Assembly;
        var domainAssembly = typeof(BasicDomainClass).Assembly;

        // Act
        services.AddCqrsBehaviours(applicationAssembly, domainAssembly, builder => { });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var integrationEventHandler = serviceProvider.GetService<IIntegrationEventHandler<ApplicationTestFixture.TestIntegrationEvent>>();
        
        integrationEventHandler.ShouldNotBeNull();
    }

    [Fact]
    public void AddCqrsBehaviours_ShouldAddDomainEventHandler_WhenPresentInApplicationAssembly()
    {
        // Arrange
        var services = new ServiceCollection();
        var applicationAssembly = typeof(BasicApplicationClass).Assembly;
        var domainAssembly = typeof(BasicDomainClass).Assembly;

        // Act
        services.AddCqrsBehaviours(applicationAssembly, domainAssembly, builder => { });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var domainEventHandler = serviceProvider.GetService<IDomainEventHandler<DomainTestFixture.TestDomainEvent>>();
        
        domainEventHandler.ShouldNotBeNull();
    }
}


public static class HandlerTestingExtensions<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_innerHandler")]
    public static extern ref ICommandHandler<TCommand, TResponse> GetInnerHandler(CommandHandlerDecorator<TCommand, TResponse> handler);
}