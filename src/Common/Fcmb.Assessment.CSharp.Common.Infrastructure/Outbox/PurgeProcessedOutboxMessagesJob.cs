using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;

[DisallowConcurrentExecution]
public sealed class PurgeProcessedOutboxMessagesJob<TUnitOfWork>(
    MessagingModule<TUnitOfWork> module,
    IServiceScopeFactory serviceScopeFactory,
    IOptionsMonitor<OutboxSettings> outboxSettings,
    TimeProvider time,
    ILogger<PurgeProcessedOutboxMessagesJob<TUnitOfWork>> logger) : IJob
    where TUnitOfWork : ICommonUnitOfWork
{
    public async ValueTask Execute(
        IJobExecutionContext context,
        CancellationToken cancellationToken = new())
    {
        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        TUnitOfWork uOw = scope.ServiceProvider.GetRequiredService<TUnitOfWork>();

        OutboxSettings outbox = outboxSettings.Get(module.Name);

        DateTimeOffset now = time.GetUtcNow();

        DateTimeOffset cutoff = now.AddDays(-outbox.RetentionDays);

        int purgedOutboxMessages = await uOw.OutboxMessagesReadRepository.DeleteProcessedBeforeAsync(cutoff);

        logger.LogInformation("Purged {PurgedCount} {Module} outbox messages processed before {Cutoff}",
            purgedOutboxMessages, module.Name, cutoff);
    }
}
