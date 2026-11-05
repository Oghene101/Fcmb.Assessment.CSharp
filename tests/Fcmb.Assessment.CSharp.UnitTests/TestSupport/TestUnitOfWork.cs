using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Common.Application.Inbox;
using Fcmb.Assessment.CSharp.Common.Application.Outbox;
using Fcmb.Assessment.CSharp.Common.Infrastructure;

namespace Fcmb.Assessment.CSharp.UnitTests.TestSupport;

internal sealed class TestUnitOfWork(
    MessagingTestDbContext db) : ICommonUnitOfWork
{
    private readonly InMemoryOutboxMessageRepository _outboxMessages = new(db);
    private readonly InMemoryInboxMessageRepository _inboxMessages = new(db);

    public IOutboxMessageRepository OutboxMessagesReadRepository => _outboxMessages;

    public IRepository<OutboxMessage> OutboxMessagesWriteRepository { get; } =
        new Repository<OutboxMessage, MessagingTestDbContext>(db);

    public IOutboxMessageConsumerRepository OutboxMessageConsumersReadRepository => _outboxMessages;

    public IRepository<OutboxMessageConsumer> OutboxMessageConsumersWriteRepository { get; } =
        new Repository<OutboxMessageConsumer, MessagingTestDbContext>(db);

    public IRepository<DeadLetteredOutboxMessage> DeadLetteredOutboxMessagesWriteRepository { get; } =
        new Repository<DeadLetteredOutboxMessage, MessagingTestDbContext>(db);

    public IInboxMessageRepository InboxMessagesReadRepository => _inboxMessages;

    public IRepository<InboxMessage> InboxMessagesWriteRepository { get; } =
        new Repository<InboxMessage, MessagingTestDbContext>(db);

    public IInboxMessageConsumerRepository InboxMessageConsumersReadRepository => _inboxMessages;

    public IRepository<InboxMessageConsumer> InboxMessageConsumersWriteRepository { get; } =
        new Repository<InboxMessageConsumer, MessagingTestDbContext>(db);

    public IRepository<DeadLetteredInboxMessage> DeadLetteredInboxMessagesWriteRepository { get; } =
        new Repository<DeadLetteredInboxMessage, MessagingTestDbContext>(db);

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
