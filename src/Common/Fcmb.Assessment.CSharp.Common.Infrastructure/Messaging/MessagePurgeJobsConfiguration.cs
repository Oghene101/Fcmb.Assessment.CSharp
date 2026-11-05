using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Inbox;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;
using Quartz;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;

public static class MessagePurgeJobsConfiguration
{
    extension(IQuartzBuilder quartz)
    {
        // Runs at start-up, then every interval.
        public IQuartzBuilder AddRecurringJob<TJob>(
            string jobName,
            TimeSpan interval)
            where TJob : IJob
        {
            quartz
                .AddJob<TJob>(job => job.WithIdentity(jobName))
                .AddTrigger(configure =>
                    configure
                        .ForJob(jobName)
                        .StartNow()
                        .WithSimpleSchedule(schedule =>
                            schedule.WithInterval(interval)
                                .RepeatForever()));

            return quartz;
        }

        public IQuartzBuilder AddMessagePurgeJobs<TUnitOfWork>(
            string module,
            OutboxSettings outbox,
            InboxSettings inbox)
            where TUnitOfWork : ICommonUnitOfWork
        {
            quartz.AddRecurringJob<PurgeProcessedOutboxMessagesJob<TUnitOfWork>>(
                $"{module}.{nameof(PurgeProcessedOutboxMessagesJob<>)}",
                TimeSpan.FromMinutes(outbox.PurgeIntervalMinutes));

            quartz.AddRecurringJob<PurgeProcessedInboxMessagesJob<TUnitOfWork>>(
                $"{module}.{nameof(PurgeProcessedInboxMessagesJob<>)}",
                TimeSpan.FromMinutes(inbox.PurgeIntervalMinutes));

            return quartz;
        }
    }
}
