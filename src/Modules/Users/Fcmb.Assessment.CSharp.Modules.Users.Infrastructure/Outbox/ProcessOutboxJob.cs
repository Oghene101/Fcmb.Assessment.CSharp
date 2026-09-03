using System.Reflection;
using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Application.Outbox;
using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Serialization;
using Fcmb.Assessment.CSharp.Modules.Users.Application.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;

namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Outbox;

#pragma warning disable CA1873
[DisallowConcurrentExecution]
internal sealed class ProcessOutboxJob(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<OutboxSettings> processOutbox,
    TimeProvider time,
    ILogger<ProcessOutboxJob> logger) : IJob
{
    private readonly OutboxSettings _processOutbox = processOutbox.Value;
    private const string Module = "Users";

    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Beginning {Module} outbox message processing", Module);

        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        IUnitOfWork uOw = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await uOw.BeginTransactionAsync(context.CancellationToken);

        Guid[] outboxMessageIds =
            await uOw.OutboxMessagesReadRepository.GetIdsAsync(_processOutbox.BatchSize);

        await uOw.CommitTransactionAsync(context.CancellationToken);

        if (outboxMessageIds.Length is 0)
        {
            logger.LogDebug("There are no {Module} outbox messages to process", Module);
            return;
        }

        foreach (Guid id in outboxMessageIds)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("{Module} Outbox processing cancelled", Module);
                break;
            }

            await ProcessMessageAsync(id, context.CancellationToken);
        }

        logger.LogInformation("{Module}Completed outbox message processing", Module);
    }

    private async Task ProcessMessageAsync(
        Guid outboxMessageId,
        CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        IUnitOfWork uOw = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await uOw.BeginTransactionAsync(cancellationToken);

        OutboxMessage outboxMessage = await uOw.OutboxMessagesReadRepository.GetByIdAsync(outboxMessageId);
        if (outboxMessage is null || outboxMessage.ProcessedOn is not null)
        {
            await uOw.RollbackTransactionAsync(cancellationToken);
            return;
        }

        Exception? exception = null;
        try
        {
            IDomainEvent domainEvent = JsonConvert.DeserializeObject<IDomainEvent>(
                outboxMessage.Content,
                SerializerSettings.Instance)!;

            Type domainEventHandlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());

            IEnumerable<object?> domainEventHandlers = scope.ServiceProvider.GetServices(domainEventHandlerType);

            foreach (object domainEventHandler in domainEventHandlers)
            {
                if (domainEventHandler is null) { continue; }

                MethodInfo handleMethod =
                    domainEventHandlerType.GetMethod(nameof(IDomainEventHandler<>.Handle))!;

                await (Task)handleMethod.Invoke(domainEventHandler, [domainEvent, cancellationToken])!;
            }

            logger.LogInformation("Successfully processed {Module} outbox message with Id '{MessageId}'", Module,
                outboxMessage.Id);
        }
        catch (Exception caughtException) when (caughtException is not OperationCanceledException)
        {
            logger.LogError(caughtException,
                "Exception while processing {Module} outbox message with Id '{MessageId}' (attempt {RetryCount})",
                Module, outboxMessage.Id, outboxMessage.RetryCount + 1);

            exception = caughtException;
        }

        await UpdateOutboxMessageAsync(uOw, outboxMessage, exception, cancellationToken);

        await uOw.SaveChangesAsync(cancellationToken);
        await uOw.CommitTransactionAsync(cancellationToken);
    }

    private async Task UpdateOutboxMessageAsync(
        IUnitOfWork uOw,
        OutboxMessage outboxMessage,
        Exception? exception,
        CancellationToken cancellationToken = default)
    {
        if (exception is null)
        {
            // Success
            outboxMessage.ProcessedOn = time.GetUtcNow();
            outboxMessage.Error = null;
            outboxMessage.NextRetryOn = null;

            uOw.OutboxMessagesWriteRepository.Update(outboxMessage,
                x => x.ProcessedOn,
                x => x.Error,
                x => x.NextRetryOn);
        }
        else
        {
            outboxMessage.RetryCount++;
            outboxMessage.Error = exception.ToString();

            if (outboxMessage.RetryCount >= _processOutbox.MaxRetries)
            {
                logger.LogCritical(
                    "{Module} Outbox message with Id '{MessageId}' exceeded max retries ({MaxRetries}). Dead-lettering.",
                    Module, outboxMessage.Id, _processOutbox.MaxRetries);

                await uOw.DeadLetteredOutboxMessagesWriteRepository.AddAsync(new DeadLetteredOutboxMessage
                {
                    Id = outboxMessage.Id,
                    Type = outboxMessage.Type,
                    Content = outboxMessage.Content,
                    OccurredOn = outboxMessage.OccurredOn,
                    DeadLetteredOn = time.GetUtcNow(),
                    RetryCount = outboxMessage.RetryCount,
                    Error = exception.ToString()
                }, cancellationToken);

                // Delete from outbox so it's no longer picked up
                uOw.OutboxMessagesWriteRepository.Delete(outboxMessage);
            }
            else
            {
                // Exponential backoff
                int delaySeconds = _processOutbox.RetryDelaySeconds * (int)Math.Pow(2, outboxMessage.RetryCount - 1);
                outboxMessage.NextRetryOn = time.GetUtcNow().AddSeconds(delaySeconds);

                logger.LogWarning(
                    "{Module} Outbox message {MessageId} scheduled for retry {RetryCount}/{MaxRetries} at {NextRetryOn}",
                    Module, outboxMessage.Id, outboxMessage.RetryCount, _processOutbox.MaxRetries,
                    outboxMessage.NextRetryOn);

                uOw.OutboxMessagesWriteRepository.Update(outboxMessage,
                    x => x.Error,
                    x => x.RetryCount,
                    x => x.NextRetryOn);
            }
        }
    }
}
#pragma warning restore CA1873
