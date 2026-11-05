namespace Fcmb.Assessment.CSharp.Common.Application.Inbox;

public sealed class InboxMessage
{
    public Guid Id { get; init; }
    public string Type { get; init; }
    public string Lane { get; init; }
    public string Content { get; set; }
    public DateTimeOffset OccurredOn { get; init; }
    public DateTimeOffset? ProcessedOn { get; set; }
    public DateTimeOffset? NextRetryOn { get; set; }

    // While set and in the future, one processor holds the message; once it lapses another may claim it.
    public DateTimeOffset? ClaimedUntil { get; set; }

    public int RetryCount { get; set; }
    public string? Error { get; set; }
}
