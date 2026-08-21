namespace Fcmb.Assessment.CSharp.Common.Application.Data;

public interface IOutboxMessageConsumerRepository
{
    Task<bool> OutboxMessageConsumerExistsAsync(Guid outboxMessageId, string name);
}
