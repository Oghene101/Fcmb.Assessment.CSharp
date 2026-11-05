using Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;

public sealed record OutboxSettings
{
    // How long a claimed message is held before another processor may take it over.
    public int LeaseSeconds { get; init; }

    public int RetentionDays { get; init; }

    public int PurgeIntervalMinutes { get; init; }

    public Dictionary<string, MessageLaneSettings> Lanes { get; init; } = [];
}
