using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;
using Quartz;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.AuditLog;

public static class AuditLogPartitionsJobConfiguration
{
    extension(IQuartzBuilder quartz)
    {
        public IQuartzBuilder AddAuditLogPartitionsJob<TUnitOfWork>(
            string module,
            AuditLogSettings auditLogs)
            where TUnitOfWork : ICommonUnitOfWork
        {
            quartz.AddRecurringJob<AuditLogPartitionsJob<TUnitOfWork>>(
                $"{module}.{nameof(AuditLogPartitionsJob<>)}",
                TimeSpan.FromMinutes(auditLogs.PartitionJobIntervalMinutes));

            return quartz;
        }
    }
}
