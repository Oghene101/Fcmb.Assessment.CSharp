using Quartz;

namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Inbox;

internal static class ConfigureProcessInboxJob
{
    public static IQuartzBuilder AddProcessInboxJob(
        this IQuartzBuilder quartz,
        InboxSettings inbox)
    {
        string jobName = typeof(ProcessInboxJob).FullName!;

        quartz
            .AddJob<ProcessInboxJob>(job => job.WithIdentity(jobName))
            .AddTrigger(configure =>
                configure
                    .ForJob(jobName)
                    .WithSimpleSchedule(schedule =>
                        schedule.WithInterval(
                                TimeSpan.FromSeconds(inbox.IntervalInSeconds))
                            .RepeatForever()));

        return quartz;
    }
}
