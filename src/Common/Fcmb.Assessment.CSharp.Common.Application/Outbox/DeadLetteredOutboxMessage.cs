namespace Fcmb.Assessment.CSharp.Common.Application.Outbox;

public sealed class DeadLetteredOutboxMessage
{
    public Guid Id { get; init; }
    public string Type { get; init; }
    public string Content { get; init; }
    public DateTimeOffset OccurredOn { get; init; }
    public DateTimeOffset DeadLetteredOn { get; set; }
    public int RetryCount { get; set; }
    public string Error { get; set; }
}
