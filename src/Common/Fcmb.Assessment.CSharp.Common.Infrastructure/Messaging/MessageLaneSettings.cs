namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;

public sealed record MessageLaneSettings
{
    // Messages of this lane handled at once, per instance.
    public int Concurrency { get; init; }

    // How often an idle lane looks for work; a busy lane claims as soon as a slot frees.
    public int PollIntervalMilliseconds { get; init; }

    public int MaxRetries { get; init; }

    public int RetryDelaySeconds { get; init; }
}
