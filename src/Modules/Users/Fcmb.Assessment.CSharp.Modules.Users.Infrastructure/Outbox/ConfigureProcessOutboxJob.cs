using Quartz;

namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Outbox;

internal static class ConfigureProcessOutboxJob
{
    public static IQuartzBuilder AddProcessOutboxJob(
        this IQuartzBuilder quartz,
        OutboxSettings outbox)
    {
        string jobName = typeof(ProcessOutboxJob).FullName!;

        quartz
            .AddJob<ProcessOutboxJob>(job => job.WithIdentity(jobName))
            .AddTrigger(configure =>
                configure
                    .ForJob(jobName)
                    .WithSimpleSchedule(schedule =>
                        schedule.WithInterval(
                                TimeSpan.FromSeconds(outbox.IntervalInSeconds))
                            .RepeatForever()));

        return quartz;
    }
}
