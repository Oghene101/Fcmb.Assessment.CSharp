namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Inbox;

internal sealed record InboxSettings
{
    public const string Path = "Transactions:Inbox";
    public int IntervalInSeconds { get; init; }
    public int BatchSize { get; init; }
    public int MaxRetries { get; init; }
    public int RetryDelaySeconds { get; init; }
}
