using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Inbox;

[DisallowConcurrentExecution]
public sealed class PurgeProcessedInboxMessagesJob<TUnitOfWork>(
    MessagingModule<TUnitOfWork> module,
    IServiceScopeFactory serviceScopeFactory,
    IOptionsMonitor<InboxSettings> inboxSettings,
    TimeProvider time,
    ILogger<PurgeProcessedInboxMessagesJob<TUnitOfWork>> logger) : IJob
    where TUnitOfWork : ICommonUnitOfWork
{
    public async ValueTask Execute(
        IJobExecutionContext context,
        CancellationToken cancellationToken = new())
    {
        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        TUnitOfWork uOw = scope.ServiceProvider.GetRequiredService<TUnitOfWork>();

        InboxSettings inbox = inboxSettings.Get(module.Name);

        DateTimeOffset now = time.GetUtcNow();

        DateTimeOffset cutoff = now.AddDays(-inbox.RetentionDays);

        int purgedInboxMessages = await uOw.InboxMessagesReadRepository.DeleteProcessedBeforeAsync(cutoff);

        logger.LogInformation("Purged {PurgedCount} {Module} inbox messages processed before {Cutoff}",
            purgedInboxMessages, module.Name, cutoff);
    }
}
