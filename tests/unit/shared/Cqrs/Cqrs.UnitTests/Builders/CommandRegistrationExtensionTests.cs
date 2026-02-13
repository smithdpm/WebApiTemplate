using System.Runtime.CompilerServices;
using Ardalis.Result;
using Cqrs.ApplicationTestFixture;
using Cqrs.Builders;
using Cqrs.Decorators;
using Cqrs.Decorators.AtomicTransactionDecorator;
using Cqrs.Decorators.IntegrationEventToOutboxDecorator;
using Cqrs.Decorators.LoggingDecorator;
using Cqrs.Decorators.Registries;
using Cqrs.Decorators.ValidationDecorator;
using Cqrs.DomainTestFixture;
using Cqrs.Events.DomainEvents;
using Cqrs.Events.IntegrationEvents;
using Cqrs.Messaging;
using Cqrs.Operations.Commands;
using Cqrs.Operations.Queries;
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
        services.AddScoped(sp => Substitute.For<ILoggingBehaviour>());
        services.AddScoped(sp => Substitute.For<IIntegrationEventToOutboxBehaviour>());
        services.AddScoped(sp => Substitute.For<IAtomicTransactionBehaviour>());

        // Act
        services.AddCqrsBehaviours(applicationAssembly, domainAssembly, builder => { });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        
        var handler1 = scope.ServiceProvider.GetRequiredService<ICommandHandler<ApplicationTestFixture.TestCommand, string>>();
        handler1.ShouldBeOfType<LoggingCommandDecorator<TestCommand, string>>();
        var handler2 = HandlerTestingExtensions<TestCommand, Result<string>>.GetInnerHandler((LoggingCommandDecorator<TestCommand, string>)handler1);
        handler2.ShouldBeOfType<ValidationCommandDecorator<TestCommand, string>>();
        var handler3 = HandlerTestingExtensions<TestCommand, Result<string>>.GetInnerHandler((ValidationCommandDecorator<TestCommand, string>)handler2);
        handler3.ShouldBeOfType<AtomicTransactionCommandDecorator<TestCommand, string>>();
        var handler4 = HandlerTestingExtensions<TestCommand, Result<string>>.GetInnerHandler((AtomicTransactionCommandDecorator<TestCommand, string>)handler3);
        handler4.ShouldBeOfType<IntegrationEventToOutboxCommandDecorator<TestCommand, string>> ();
        var handler5 = HandlerTestingExtensions<TestCommand, Result<string>>.GetInnerHandler((IntegrationEventToOutboxCommandDecorator<TestCommand, string>)handler4);
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

        services.AddFakeLogging();
        services.AddScoped(sp => Substitute.For<IRepository<OutboxMessage>>());
        services.AddScoped(sp => Substitute.For<IUnitOfWork>());

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

        services.AddFakeLogging();
        services.AddScoped(sp => Substitute.For<IRepository<OutboxMessage>>());
        services.AddScoped(sp => Substitute.For<IUnitOfWork>());

        // Act
        services.AddCqrsBehaviours(applicationAssembly, domainAssembly, builder => { });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var domainEventHandler = serviceProvider.GetService<IDomainEventHandler<DomainTestFixture.TestDomainEvent>>();
        
        domainEventHandler.ShouldNotBeNull();
    }
}


public static class HandlerTestingExtensions<TInput, TResult>
    where TResult : IResult
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_innerHandler")]
    public static extern ref IHandler<TInput, TResult> GetInnerHandler(HandlerDecorator<TInput, TResult> handler);
}