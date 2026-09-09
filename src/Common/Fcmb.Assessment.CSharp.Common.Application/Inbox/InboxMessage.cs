namespace Fcmb.Assessment.CSharp.Common.Application.Inbox;

public sealed class InboxMessage
{
    public Guid Id { get; init; }
    public string Type { get; init; }
    public string Content { get; init; }
    public DateTimeOffset OccurredOn { get; init; }
    public DateTimeOffset? ProcessedOn { get; set; }
    public DateTimeOffset? NextRetryOn { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}
