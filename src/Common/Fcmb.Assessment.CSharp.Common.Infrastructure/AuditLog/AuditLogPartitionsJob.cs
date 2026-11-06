using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.AuditLog;

[DisallowConcurrentExecution]
public sealed class AuditLogPartitionsJob<TUnitOfWork>(
    MessagingModule<TUnitOfWork> module,
    IServiceScopeFactory serviceScopeFactory,
    IOptionsMonitor<AuditLogSettings> auditLogSettings,
    TimeProvider time,
    ILogger<AuditLogPartitionsJob<TUnitOfWork>> logger) : IJob
    where TUnitOfWork : ICommonUnitOfWork
{
    public async ValueTask Execute(
        IJobExecutionContext context,
        CancellationToken cancellationToken = new())
    {
        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        TUnitOfWork uOw = scope.ServiceProvider.GetRequiredService<TUnitOfWork>();

        AuditLogSettings auditLogs = auditLogSettings.Get(module.Name);

        DateTimeOffset now = time.GetUtcNow();

        var currentMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        await uOw.AuditLogPartitionsRepository.CreateMonthsAheadAsync(currentMonth, auditLogs.PartitionMonthsAhead);

        if (auditLogs.RetentionMonths <= 0)
        {
            return;
        }

        DateTimeOffset keepFrom = currentMonth.AddMonths(-auditLogs.RetentionMonths);

        int droppedMonths = await uOw.AuditLogPartitionsRepository.DropMonthsBeforeAsync(keepFrom);

        logger.LogInformation("Dropped {DroppedMonths} {Module} audit log months before {KeepFrom}",
            droppedMonths, module.Name, keepFrom);
    }
}
