namespace Fcmb.Assessment.CSharp.Common.Application.Outbox;

public sealed class OutboxMessageConsumer
{
    public Guid OutboxMessageId { get; init; }

    public string Name { get; init; }
}
