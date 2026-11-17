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

// Handlers run in one scope, and on success their work, their handled-records and the processed message are saved
// together. A failure is recorded in a fresh scope, so whatever a failed handler left tracked is never saved with it,
// and every handler runs again on the retry. The claim's lease, not a lock, keeps other processors away meanwhile.
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
        await using AsyncServiceScope handlingScope = serviceScopeFactory.CreateAsyncScope();
        TUnitOfWork handlingUnitOfWork = handlingScope.ServiceProvider.GetRequiredService<TUnitOfWork>();

        OutboxMessage? outboxMessage = await handlingUnitOfWork.OutboxMessagesReadRepository.GetUnprocessedByIdAsync(
            outboxMessageId);

        if (outboxMessage is null)
        {
            return;
        }

        Result outcome = await HandleAsync(handlingScope.ServiceProvider, outboxMessage, cancellationToken);

        if (outcome.IsSuccess)
        {
            outcome = await SaveProcessedAsync(handlingUnitOfWork, outboxMessage, cancellationToken);
        }

        if (outcome.IsSuccess)
        {
            return;
        }

        await RecordFailureAsync(outboxMessageId, outcome.Error!, laneSettings, cancellationToken);
    }

    private async Task<Result> HandleAsync(
        IServiceProvider services,
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken)
    {
        Result<IDomainEvent> domainEvent = Read(outboxMessage);

        if (!domainEvent.IsSuccess)
        {
            return Result.Failure(domainEvent.Error!);
        }

        Result handled = await InvokeHandlersAsync(services, domainEvent.Value, cancellationToken);

        return handled;
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

    private async Task<Result> SaveProcessedAsync(
        TUnitOfWork handlingUnitOfWork,
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken)
    {
        outboxMessage.Content = ClearedContent;
        outboxMessage.ProcessedOn = time.GetUtcNow();
        outboxMessage.ClaimedUntil = null;
        outboxMessage.Error = null;
        outboxMessage.NextRetryOn = null;

        handlingUnitOfWork.OutboxMessagesWriteRepository.Update(outboxMessage,
            x => x.Content,
            x => x.ProcessedOn,
            x => x.ClaimedUntil,
            x => x.Error,
            x => x.NextRetryOn);

        try
        {
            await handlingUnitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return MessageErrors.SaveFailed(exception);
        }

        logger.LogInformation("Processed {Module} outbox message {MessageId}", module.Name, outboxMessage.Id);

        return Result.Success();
    }

    // A Dependency failure is retried with backoff until MaxRetries; any other failure is final. The message is read
    // again because the handling scope may have changed it before its save failed.
    private async Task RecordFailureAsync(
        Guid outboxMessageId,
        Error failure,
        MessageLaneSettings laneSettings,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope recordingScope = serviceScopeFactory.CreateAsyncScope();
        TUnitOfWork uOw = recordingScope.ServiceProvider.GetRequiredService<TUnitOfWork>();

        OutboxMessage? outboxMessage = await uOw.OutboxMessagesReadRepository.GetUnprocessedByIdAsync(outboxMessageId);

        if (outboxMessage is null)
        {
            return;
        }

        outboxMessage.RetryCount++;
        outboxMessage.Error = failure.Message;

        bool retryable = failure.Type == ErrorType.Dependency && outboxMessage.RetryCount < laneSettings.MaxRetries;

        if (retryable)
        {
            ScheduleRetry(uOw, outboxMessage, laneSettings);
        }
        else
        {
            await DeadLetterAsync(uOw, outboxMessage, failure, cancellationToken);
        }

        await uOw.SaveChangesAsync(cancellationToken);
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
