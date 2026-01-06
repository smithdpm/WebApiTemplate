

using Application.Abstractions.Events;
using Application.Behaviours.Registries;
using Ardalis.Result;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedKernel.Database;
using SharedKernel.Events;
using SharedKernel.Events.DomainEvents;
using SharedKernel.Events.IntegrationEvents;
using System.Text.Json;

namespace Infrastructure.Database;
internal class OutboxDispatcher : BackgroundService
{
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IEventTypeRegistry _eventTypeRegistry;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly IIntegrationEventDispatcher _integrationEventDispatcher;
    private int _maxProcessingAttempts = 3;
    private int _batchSize = 10;
    private int _lockDuration = 60;

    public OutboxDispatcher(
        ILogger<OutboxDispatcher> logger,
        IOutboxRepository outboxRepository,
        IEventTypeRegistry eventTypeRegistry,
        IDomainEventDispatcher domainEventDispatcher,
        IIntegrationEventDispatcher integrationEventDispatcher)
    {
        _logger = logger;
        _outboxRepository = outboxRepository;
        _eventTypeRegistry = eventTypeRegistry;
        _domainEventDispatcher = domainEventDispatcher;
        _integrationEventDispatcher = integrationEventDispatcher;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var messages = await _outboxRepository.FetchOutboxMessagesForProcessing(_batchSize, _lockDuration, cancellationToken);

            foreach (var message in messages)
            {
                try
                {
                    if (message.Destination is null)
                    {
                        await ProcessDomainEventMessage(message, cancellationToken);
                    }
                    else
                    {
                        await ProcessIntegrationEventMessage(message, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occured while proccessing message Id {OutboxMessageId}. Error: {errorMessage}.",
                            message.Id, ex.Message);
                    if (message.ProcessingAttempts >= _maxProcessingAttempts -1)
                    {
                        _logger.LogError(ex, "Outbox message with Id {OutboxMessageId} has exceeded max processing attempts. Marking as errored.",
                            message.Id);
                        await _outboxRepository.MarkMessageAsErrored(message.Id, ex.Message, cancellationToken);
                    }
                    else
                    {
                        await _outboxRepository.MarkMessageForRetry(message.Id, cancellationToken);
                    }
                }
            }
        }
    }

    private async Task ProcessDomainEventMessage(OutboxMessage message, CancellationToken cancellationToken)
    {
        var domainEventResult = ParseEvent<IDomainEvent>(message);
        if (domainEventResult.IsError())
        {
            _logger.LogError("Failed to parse domain event from outbox message with Id {OutboxMessageId}. Error: {Error}",
                message.Id, domainEventResult.Errors);
            await _outboxRepository.MarkMessageAsErrored(message.Id, domainEventResult.Errors.First(), cancellationToken);
            return;
        }

        if (domainEventResult.IsSuccess)
        {
            await _domainEventDispatcher.DispatchEventsAsync(
                new List<IDomainEvent>() { domainEventResult.Value },
                cancellationToken);
            await _outboxRepository.MarkMessageAsCompleted(message.Id, cancellationToken);
        }
    }
    private async Task ProcessIntegrationEventMessage(OutboxMessage message, CancellationToken cancellationToken)
    {
        var integrationEventResult = ParseEvent<IntegrationEventBase>(message);
        if (integrationEventResult.IsError())
        {
            _logger.LogError("Failed to parse integration event from outbox message with Id {OutboxMessageId}. Error: {Error}",
                message.Id, integrationEventResult.Errors);
            await _outboxRepository.MarkMessageAsErrored(message.Id, integrationEventResult.Errors.First(), cancellationToken);
            return;
        }
        if (integrationEventResult.IsSuccess)
        {
            await _integrationEventDispatcher.DispatchEventsAsync(
                integrationEvents: new List<IntegrationEventBase>() { integrationEventResult.Value },
                queueOrTopic: message.Destination!,
                cancellationToken);
            await _outboxRepository.MarkMessageAsCompleted(message.Id, cancellationToken);
        }
    }
    private Result<TEvent> ParseEvent<TEvent>(OutboxMessage message)
    {
        var eventType = _eventTypeRegistry.GetTypeByName(message.EventType);

        if (eventType == null)
        {
            return Result<TEvent>.Error(
                $"The domain event type {message.EventType} is not a registered domain event for this service.");
        }

        try
        {
            var deserializedEvent = (TEvent?)JsonSerializer.Deserialize(
                    message.Payload,
                    eventType);

            if (deserializedEvent == null)
            {
                return Result<TEvent>.Error(
                        $"Failed to deserialize CloudEvent data to type {eventType.Name}.");
            }
            return Result<TEvent>.Success(deserializedEvent);
        }
        catch (JsonException jsonEx)
        {
            return Result<TEvent>.Error(
                $"JSON Deserialization failed for domain event type {message.EventType}. ExceptionMessage: {jsonEx.Message}"
                );
        }
    }
}
