using System.Reflection;
using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Application.Inbox;
using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Inbox;

// Handlers run in one scope and the outcome is recorded in a fresh one, so whatever a failed handler left
// tracked is never saved with it. The claim's lease, not a lock, keeps other processors away meanwhile.
public sealed class InboxMessageProcessor<TUnitOfWork>(
    MessagingModule<TUnitOfWork> module,
    IServiceScopeFactory serviceScopeFactory,
    TimeProvider time,
    ILogger<InboxMessageProcessor<TUnitOfWork>> logger)
    where TUnitOfWork : ICommonUnitOfWork
{
    // Events carry customer details that are not needed once handled.
    private const string ClearedContent = "{}";

    public async Task ProcessAsync(
        Guid inboxMessageId,
        MessageLaneSettings laneSettings,
        CancellationToken cancellationToken)
    {
        HandledInboxMessage? handledInboxMessage = await HandleAsync(inboxMessageId, cancellationToken);

        if (handledInboxMessage is null)
        {
            return;
        }

        await RecordOutcomeAsync(handledInboxMessage, laneSettings, cancellationToken);
    }

    private sealed record HandledInboxMessage(
        InboxMessage InboxMessage,
        Result Outcome);

    private async Task<HandledInboxMessage?> HandleAsync(
        Guid inboxMessageId,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope handlingScope = serviceScopeFactory.CreateAsyncScope();
        TUnitOfWork uOw = handlingScope.ServiceProvider.GetRequiredService<TUnitOfWork>();

        InboxMessage? inboxMessage = await uOw.InboxMessagesReadRepository.GetUnprocessedByIdAsync(inboxMessageId);

        if (inboxMessage is null)
        {
            return null;
        }

        Result<IIntegrationEvent> integrationEvent = Read(inboxMessage);

        if (!integrationEvent.IsSuccess)
        {
            var unreadable = Result.Failure(integrationEvent.Error!);

            return new HandledInboxMessage(inboxMessage, unreadable);
        }

        Result outcome = await InvokeHandlersAsync(handlingScope.ServiceProvider, integrationEvent.Value, cancellationToken);

        return new HandledInboxMessage(inboxMessage, outcome);
    }

    private Result<IIntegrationEvent> Read(InboxMessage inboxMessage)
    {
        try
        {
            IIntegrationEvent integrationEvent = JsonConvert.DeserializeObject<IIntegrationEvent>(
                inboxMessage.Content,
                SerializerSettings.Instance)!;

            return Result.Success(integrationEvent);
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "{Module} inbox message {MessageId} could not be read", module.Name,
                inboxMessage.Id);

            return Result.Failure<IIntegrationEvent>(MessageErrors.Unreadable(exception));
        }
    }

    // The first failure stops the remaining handlers; they all run again on the retry, behind their idempotency.
    private static async Task<Result> InvokeHandlersAsync(
        IServiceProvider services,
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        Type handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(integrationEvent.GetType());

        MethodInfo handleMethod = handlerType.GetMethod(nameof(IIntegrationEventHandler<>.Handle))!;

        IEnumerable<object?> handlers = services.GetServices(handlerType);

        try
        {
            foreach (object? handler in handlers)
            {
                var handling = (Task<Result>)handleMethod.Invoke(handler, [integrationEvent, cancellationToken])!;

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
        HandledInboxMessage handledInboxMessage,
        MessageLaneSettings laneSettings,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope recordingScope = serviceScopeFactory.CreateAsyncScope();
        TUnitOfWork uOw = recordingScope.ServiceProvider.GetRequiredService<TUnitOfWork>();

        InboxMessage inboxMessage = handledInboxMessage.InboxMessage;
        Result outcome = handledInboxMessage.Outcome;

        if (outcome.IsSuccess)
        {
            MarkProcessed(uOw, inboxMessage);
        }
        else
        {
            await RecordFailureAsync(uOw, inboxMessage, outcome.Error!, laneSettings, cancellationToken);
        }

        await uOw.SaveChangesAsync(cancellationToken);
    }

    private void MarkProcessed(
        TUnitOfWork uOw,
        InboxMessage inboxMessage)
    {
        inboxMessage.Content = ClearedContent;
        inboxMessage.ProcessedOn = time.GetUtcNow();
        inboxMessage.ClaimedUntil = null;
        inboxMessage.Error = null;
        inboxMessage.NextRetryOn = null;

        uOw.InboxMessagesWriteRepository.Update(inboxMessage,
            x => x.Content,
            x => x.ProcessedOn,
            x => x.ClaimedUntil,
            x => x.Error,
            x => x.NextRetryOn);

        logger.LogInformation("Processed {Module} inbox message {MessageId}", module.Name, inboxMessage.Id);
    }

    // A Dependency failure is retried with backoff until MaxRetries; any other failure is final.
    private async Task RecordFailureAsync(
        TUnitOfWork uOw,
        InboxMessage inboxMessage,
        Error failure,
        MessageLaneSettings laneSettings,
        CancellationToken cancellationToken)
    {
        inboxMessage.RetryCount++;
        inboxMessage.Error = failure.Message;

        bool retryable = failure.Type == ErrorType.Dependency && inboxMessage.RetryCount < laneSettings.MaxRetries;

        if (retryable)
        {
            ScheduleRetry(uOw, inboxMessage, laneSettings);

            return;
        }

        await DeadLetterAsync(uOw, inboxMessage, failure, cancellationToken);
    }

    private void ScheduleRetry(
        TUnitOfWork uOw,
        InboxMessage inboxMessage,
        MessageLaneSettings laneSettings)
    {
        int delaySeconds = laneSettings.RetryDelaySeconds * (int)Math.Pow(2, inboxMessage.RetryCount - 1);

        inboxMessage.ClaimedUntil = null;
        inboxMessage.NextRetryOn = time.GetUtcNow().AddSeconds(delaySeconds);

        uOw.InboxMessagesWriteRepository.Update(inboxMessage,
            x => x.RetryCount,
            x => x.Error,
            x => x.ClaimedUntil,
            x => x.NextRetryOn);

        logger.LogWarning(
            "{Module} inbox message {MessageId} scheduled for retry {RetryCount}/{MaxRetries} at {NextRetryOn}",
            module.Name, inboxMessage.Id, inboxMessage.RetryCount, laneSettings.MaxRetries,
            inboxMessage.NextRetryOn);
    }

    private async Task DeadLetterAsync(
        TUnitOfWork uOw,
        InboxMessage inboxMessage,
        Error failure,
        CancellationToken cancellationToken)
    {
        logger.LogCritical(
            "{Module} inbox message {MessageId} failed with {ErrorCode} after {RetryCount} attempts. Dead-lettering.",
            module.Name, inboxMessage.Id, failure.Code, inboxMessage.RetryCount);

        var deadLetteredInboxMessage = new DeadLetteredInboxMessage
        {
            Id = inboxMessage.Id,
            Type = inboxMessage.Type,
            Content = inboxMessage.Content,
            OccurredOn = inboxMessage.OccurredOn,
            DeadLetteredOn = time.GetUtcNow(),
            RetryCount = inboxMessage.RetryCount,
            Error = failure.Message
        };

        await uOw.DeadLetteredInboxMessagesWriteRepository.AddAsync(deadLetteredInboxMessage, cancellationToken);

        uOw.InboxMessagesWriteRepository.Delete(inboxMessage);
    }
}
