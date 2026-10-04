namespace Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure.Outbox;

internal sealed record OutboxSettings
{
    public const string Path = "Users:Outbox";
    public int IntervalInSeconds { get; init; }
    public int BatchSize { get; init; }
    public int MaxRetries { get; init; }
    public int RetryDelaySeconds { get; init; }
}
