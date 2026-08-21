using Fcmb.Assessment.CSharp.Common.Application.Outbox;

namespace Fcmb.Assessment.CSharp.Common.Application.Data;

public interface ICommonUnitOfWork
{
    IOutboxMessageRepository OutboxMessagesReadRepository { get; }
    IRepository<OutboxMessage> OutboxMessagesWriteRepository { get; }
    IOutboxMessageConsumerRepository OutboxMessageConsumersReadRepository { get; }
    IRepository<OutboxMessageConsumer> OutboxMessageConsumersWriteRepository { get; }
    IRepository<DeadLetteredOutboxMessage> DeadLetteredOutboxMessagesWriteRepository { get; }

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
