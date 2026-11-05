using System.Reflection;
using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Application.Outbox;
using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;

// Handlers run in one scope and the outcome is recorded in a fresh one, so whatever a failed handler left
// tracked is never saved with it. The claim's lease, not a lock, keeps other processors away meanwhile.
public sealed class OutboxMessageProcessor<TUnitOfWork>(
    MessagingModule<TUnitOfWork> module,
    IServiceScopeFactory serviceScopeFactory,
    TimeProvider time,
    ILogger<OutboxMessageProcessor<TUnitOfWork>> logger)
    where TUnitOfWork : ICommonUnitOfWork
{
    // Events carry customer details that are not needed once handled.
    private const string ClearedContent = "{}";

    public async Task ProcessAsync(
        Guid outboxMessageId,
        MessageLaneSettings laneSettings,
        CancellationToken cancellationToken)
    {
        HandledOutboxMessage? handledOutboxMessage = await HandleAsync(outboxMessageId, cancellationToken);

        if (handledOutboxMessage is null)
        {
            return;
        }

        await RecordOutcomeAsync(handledOutboxMessage, laneSettings, cancellationToken);
    }

    private sealed record HandledOutboxMessage(
        OutboxMessage OutboxMessage,
        Result Outcome);

    private async Task<HandledOutboxMessage?> HandleAsync(
        Guid outboxMessageId,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope handlingScope = serviceScopeFactory.CreateAsyncScope();
        TUnitOfWork uOw = handlingScope.ServiceProvider.GetRequiredService<TUnitOfWork>();

        OutboxMessage? outboxMessage = await uOw.OutboxMessagesReadRepository.GetUnprocessedByIdAsync(outboxMessageId);

        if (outboxMessage is null)
        {
            return null;
        }

        Result<IDomainEvent> domainEvent = Read(outboxMessage);

        if (!domainEvent.IsSuccess)
        {
            var unreadable = Result.Failure(domainEvent.Error!);

            return new HandledOutboxMessage(outboxMessage, unreadable);
        }

        Result outcome = await InvokeHandlersAsync(handlingScope.ServiceProvider, domainEvent.Value, cancellationToken);

        return new HandledOutboxMessage(outboxMessage, outcome);
    }

    private Result<IDomainEvent> Read(OutboxMessage outboxMessage)
    {
        try
        {
            IDomainEvent domainEvent = JsonConvert.DeserializeObject<IDomainEvent>(
                outboxMessage.Content,
                SerializerSettings.Instance)!;

            return Result.Success(domainEvent);
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "{Module} outbox message {MessageId} could not be read", module.Name,
                outboxMessage.Id);

            return Result.Failure<IDomainEvent>(MessageErrors.Unreadable(exception));
        }
    }

    // The first failure stops the remaining handlers; they all run again on the retry, behind their idempotency.
    private static async Task<Result> InvokeHandlersAsync(
        IServiceProvider services,
        IDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        Type handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());

        MethodInfo handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<>.Handle))!;

        IEnumerable<object?> handlers = services.GetServices(handlerType);

        try
        {
            foreach (object? handler in handlers)
            {
                var handling = (Task<Result>)handleMethod.Invoke(handler, [domainEvent, cancellationToken])!;

                Result handled = await handling;

                if (!handled.IsSuccess)
                {
                    return handled;
                }
            }

            return Result.Success();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return MessageErrors.HandlerThrew(exception);
        }
    }

    private async Task RecordOutcomeAsync(
        HandledOutboxMessage handledOutboxMessage,
        MessageLaneSettings laneSettings,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope recordingScope = serviceScopeFactory.CreateAsyncScope();
        TUnitOfWork uOw = recordingScope.ServiceProvider.GetRequiredService<TUnitOfWork>();

        OutboxMessage outboxMessage = handledOutboxMessage.OutboxMessage;
        Result outcome = handledOutboxMessage.Outcome;

        if (outcome.IsSuccess)
        {
            MarkProcessed(uOw, outboxMessage);
        }
        else
        {
            await RecordFailureAsync(uOw, outboxMessage, outcome.Error!, laneSettings, cancellationToken);
        }

        await uOw.SaveChangesAsync(cancellationToken);
    }

    private void MarkProcessed(
        TUnitOfWork uOw,
        OutboxMessage outboxMessage)
    {
        outboxMessage.Content = ClearedContent;
        outboxMessage.ProcessedOn = time.GetUtcNow();
        outboxMessage.ClaimedUntil = null;
        outboxMessage.Error = null;
        outboxMessage.NextRetryOn = null;

        uOw.OutboxMessagesWriteRepository.Update(outboxMessage,
            x => x.Content,
            x => x.ProcessedOn,
            x => x.ClaimedUntil,
            x => x.Error,
            x => x.NextRetryOn);

        logger.LogInformation("Processed {Module} outbox message {MessageId}", module.Name, outboxMessage.Id);
    }

    // A Dependency failure is retried with backoff until MaxRetries; any other failure is final.
    private async Task RecordFailureAsync(
        TUnitOfWork uOw,
        OutboxMessage outboxMessage,
        Error failure,
        MessageLaneSettings laneSettings,
        CancellationToken cancellationToken)
    {
        outboxMessage.RetryCount++;
        outboxMessage.Error = failure.Message;

        bool retryable = failure.Type == ErrorType.Dependency && outboxMessage.RetryCount < laneSettings.MaxRetries;

        if (retryable)
        {
            ScheduleRetry(uOw, outboxMessage, laneSettings);

            return;
        }

        await DeadLetterAsync(uOw, outboxMessage, failure, cancellationToken);
    }

    private void ScheduleRetry(
        TUnitOfWork uOw,
        OutboxMessage outboxMessage,
        MessageLaneSettings laneSettings)
    {
        int delaySeconds = laneSettings.RetryDelaySeconds * (int)Math.Pow(2, outboxMessage.RetryCount - 1);

        outboxMessage.ClaimedUntil = null;
        outboxMessage.NextRetryOn = time.GetUtcNow().AddSeconds(delaySeconds);

        uOw.OutboxMessagesWriteRepository.Update(outboxMessage,
            x => x.RetryCount,
            x => x.Error,
            x => x.ClaimedUntil,
            x => x.NextRetryOn);

        logger.LogWarning(
            "{Module} outbox message {MessageId} scheduled for retry {RetryCount}/{MaxRetries} at {NextRetryOn}",
            module.Name, outboxMessage.Id, outboxMessage.RetryCount, laneSettings.MaxRetries,
            outboxMessage.NextRetryOn);
    }

    private async Task DeadLetterAsync(
        TUnitOfWork uOw,
        OutboxMessage outboxMessage,
        Error failure,
        CancellationToken cancellationToken)
    {
        logger.LogCritical(
            "{Module} outbox message {MessageId} failed with {ErrorCode} after {RetryCount} attempts. Dead-lettering.",
            module.Name, outboxMessage.Id, failure.Code, outboxMessage.RetryCount);

        var deadLetteredOutboxMessage = new DeadLetteredOutboxMessage
        {
            Id = outboxMessage.Id,
            Type = outboxMessage.Type,
            Content = outboxMessage.Content,
            OccurredOn = outboxMessage.OccurredOn,
            DeadLetteredOn = time.GetUtcNow(),
            RetryCount = outboxMessage.RetryCount,
            Error = failure.Message
        };

        await uOw.DeadLetteredOutboxMessagesWriteRepository.AddAsync(deadLetteredOutboxMessage, cancellationToken);

        uOw.OutboxMessagesWriteRepository.Delete(outboxMessage);
    }
}
