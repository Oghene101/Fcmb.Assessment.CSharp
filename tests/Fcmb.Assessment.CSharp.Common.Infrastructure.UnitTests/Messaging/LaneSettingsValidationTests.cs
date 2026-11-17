using Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;
using Fcmb.Assessment.CSharp.Common.Infrastructure.UnitTests.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.UnitTests.Messaging;

public sealed class LaneSettingsValidationTests
{
    private const string Module = "Tests";
    private const string SettingsPath = "Tests:Outbox";

    [Fact]
    public void AMaxPollIntervalShorterThanThePollInterval_IsRefused()
    {
        using ServiceProvider provider = OutboxServices(pollIntervalMilliseconds: 500, maxPollIntervalMilliseconds: 100);
        IOptionsMonitor<OutboxSettings> outboxSettings = provider.GetRequiredService<IOptionsMonitor<OutboxSettings>>();

        OptionsValidationException refusal = Should.Throw<OptionsValidationException>(() => outboxSettings.Get(Module));

        refusal.Message.ShouldContain("MaxPollIntervalMilliseconds");
    }

    [Fact]
    public void AMaxPollIntervalNoShorterThanThePollInterval_IsAccepted()
    {
        using ServiceProvider provider = OutboxServices(pollIntervalMilliseconds: 500, maxPollIntervalMilliseconds: 5000);
        IOptionsMonitor<OutboxSettings> outboxSettings = provider.GetRequiredService<IOptionsMonitor<OutboxSettings>>();

        OutboxSettings outbox = outboxSettings.Get(Module);

        outbox.Lanes[MessageLanes.Default].MaxPollIntervalMilliseconds.ShouldBe(5000);
    }

    private static ServiceProvider OutboxServices(
        int pollIntervalMilliseconds,
        int maxPollIntervalMilliseconds)
    {
        const string lane = $"{SettingsPath}:Lanes:{MessageLanes.Default}";

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{SettingsPath}:LeaseSeconds"] = "120",
                [$"{SettingsPath}:RetentionDays"] = "7",
                [$"{SettingsPath}:PurgeIntervalMinutes"] = "60",
                [$"{lane}:Concurrency"] = "1",
                [$"{lane}:PollIntervalMilliseconds"] = $"{pollIntervalMilliseconds}",
                [$"{lane}:MaxPollIntervalMilliseconds"] = $"{maxPollIntervalMilliseconds}",
                [$"{lane}:MaxRetries"] = "3",
                [$"{lane}:RetryDelaySeconds"] = "30"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddSingleton(configuration);
        services.AddOutbox<TestUnitOfWork>(Module, SettingsPath, [MessageLanes.Default], typeof(TestUnitOfWork).Assembly);

        ServiceProvider provider = services.BuildServiceProvider();

        return provider;
    }
}
