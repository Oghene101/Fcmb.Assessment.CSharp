using System.Reflection;
using Fcmb.Assessment.CSharp.Common.Application.Inbox;
using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Serialization;
using Fcmb.Assessment.CSharp.Modules.Users.Application.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;

namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Inbox;

#pragma warning disable CA1873
[DisallowConcurrentExecution]
internal sealed class ProcessInboxJob(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<InboxSettings> processInbox,
    TimeProvider time,
    ILogger<ProcessInboxJob> logger) : IJob
{
    private readonly InboxSettings _processInbox = processInbox.Value;
    private const string Module = "Transactions";

    public async ValueTask Execute(IJobExecutionContext context,
        CancellationToken cancellationToken = new())
    {
        logger.LogInformation("Beginning {Module} inbox message processing", Module);

        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        IUnitOfWork uOw = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await uOw.BeginTransactionAsync(context.CancellationToken);

        Guid[] inboxMessageIds =
            await uOw.InboxMessagesReadRepository.GetIdsAsync(_processInbox.BatchSize);

        await uOw.CommitTransactionAsync(context.CancellationToken);

        if (inboxMessageIds.Length is 0)
        {
            logger.LogDebug("There are no {Module} inbox messages to process", Module);
            return;
        }

        foreach (Guid id in inboxMessageIds)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("{Module} Inbox processing cancelled", Module);
                break;
            }

            await ProcessMessageAsync(id, context.CancellationToken);
        }

        logger.LogInformation("{Module}Completed inbox message processing", Module);
    }

    private async Task ProcessMessageAsync(
        Guid inboxMessageId,
        CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        IUnitOfWork uOw = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await uOw.BeginTransactionAsync(cancellationToken);

        InboxMessage inboxMessage = await uOw.InboxMessagesReadRepository.GetByIdAsync(inboxMessageId);
        if (inboxMessage is null || inboxMessage.ProcessedOn is not null)
        {
            await uOw.RollbackTransactionAsync(cancellationToken);
            return;
        }

        Exception? exception = null;
        try
        {
            IIntegrationEvent integrationEvent = JsonConvert.DeserializeObject<IIntegrationEvent>(
                inboxMessage.Content,
                SerializerSettings.Instance)!;

            Type integrationEventHandlerType =
                typeof(IIntegrationEventHandler<>).MakeGenericType(integrationEvent.GetType());

            IEnumerable<object?> integrationEventHandlers =
                scope.ServiceProvider.GetServices(integrationEventHandlerType);

            foreach (object integrationEventHandler in integrationEventHandlers)
            {
                if (integrationEventHandler is null) { continue; }

                MethodInfo handleMethod =
                    integrationEventHandlerType.GetMethod(nameof(IIntegrationEventHandler<>.Handle))!;

                await (Task)handleMethod.Invoke(integrationEventHandler, [integrationEvent, cancellationToken])!;
            }

            logger.LogInformation("Successfully processed {Module} inbox message with Id '{MessageId}'", Module,
                inboxMessage.Id);
        }
        catch (Exception caughtException) when (caughtException is not OperationCanceledException)
        {
            logger.LogError(caughtException,
                "Exception while processing {Module} inbox message with Id '{MessageId}' (attempt {RetryCount})",
                Module, inboxMessage.Id, inboxMessage.RetryCount + 1);

            exception = caughtException;
        }

        await UpdateInboxMessageAsync(uOw, inboxMessage, exception, cancellationToken);

        await uOw.SaveChangesAsync(cancellationToken);
        await uOw.CommitTransactionAsync(cancellationToken);
    }

    private async Task UpdateInboxMessageAsync(
        IUnitOfWork uOw,
        InboxMessage inboxMessage,
        Exception? exception,
        CancellationToken cancellationToken = default)
    {
        if (exception is null)
        {
            // Success
            inboxMessage.ProcessedOn = time.GetUtcNow();
            inboxMessage.Error = null;
            inboxMessage.NextRetryOn = null;

            uOw.InboxMessagesWriteRepository.Update(inboxMessage,
                x => x.ProcessedOn,
                x => x.Error,
                x => x.NextRetryOn);
        }
        else
        {
            inboxMessage.RetryCount++;
            inboxMessage.Error = exception.ToString();

            if (inboxMessage.RetryCount >= _processInbox.MaxRetries)
            {
                logger.LogCritical(
                    "{Module} Inbox message with Id '{MessageId}' exceeded max retries ({MaxRetries}). Dead-lettering.",
                    Module, inboxMessage.Id, _processInbox.MaxRetries);

                await uOw.DeadLetteredInboxMessagesWriteRepository.AddAsync(new DeadLetteredInboxMessage
                {
                    Id = inboxMessage.Id,
                    Type = inboxMessage.Type,
                    Content = inboxMessage.Content,
                    OccurredOn = inboxMessage.OccurredOn,
                    DeadLetteredOn = time.GetUtcNow(),
                    RetryCount = inboxMessage.RetryCount,
                    Error = exception.ToString()
                }, cancellationToken);

                // Delete from inbox so it's no longer picked up
                uOw.InboxMessagesWriteRepository.Delete(inboxMessage);
            }
            else
            {
                // Exponential backoff
                int delaySeconds = _processInbox.RetryDelaySeconds * (int)Math.Pow(2, inboxMessage.RetryCount - 1);
                inboxMessage.NextRetryOn = time.GetUtcNow().AddSeconds(delaySeconds);

                logger.LogWarning(
                    "{Module} Inbox message {MessageId} scheduled for retry {RetryCount}/{MaxRetries} at {NextRetryOn}",
                    Module, inboxMessage.Id, inboxMessage.RetryCount, _processInbox.MaxRetries,
                    inboxMessage.NextRetryOn);

                uOw.InboxMessagesWriteRepository.Update(inboxMessage,
                    x => x.Error,
                    x => x.RetryCount,
                    x => x.NextRetryOn);
            }
        }
    }
}
#pragma warning restore CA1873
