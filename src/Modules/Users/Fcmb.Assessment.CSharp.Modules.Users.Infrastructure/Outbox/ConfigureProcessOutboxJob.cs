using Microsoft.Extensions.Options;
using Quartz;

namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Outbox;

internal sealed class ConfigureProcessOutboxJob(IOptions<OutboxSettings> outboxOptions)
    : IConfigureOptions<QuartzOptions>
{
    private readonly OutboxSettings _outboxSettings = outboxOptions.Value;

    public void Configure(QuartzOptions options)
    {
        string jobName = typeof(ProcessOutboxJob).FullName!;

        options
            .AddJob<ProcessOutboxJob>(configure => configure.WithIdentity(jobName))
            .AddTrigger(configure =>
                configure
                    .ForJob(jobName)
                    .WithSimpleSchedule(schedule =>
                        schedule.WithIntervalInSeconds(_outboxSettings.IntervalInSeconds).RepeatForever()));
    }
}
