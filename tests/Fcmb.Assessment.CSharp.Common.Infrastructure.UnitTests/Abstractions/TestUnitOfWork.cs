using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Common.Application.Inbox;
using Fcmb.Assessment.CSharp.Common.Application.Outbox;
using Fcmb.Assessment.CSharp.Common.Infrastructure;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.UnitTests.Abstractions;

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

    public IAuditLogPartitionRepository AuditLogPartitionsRepository { get; } = new NoAuditLogPartitions();

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}

// The in-memory provider has no partitions; the messaging tests never maintain any.
file sealed class NoAuditLogPartitions : IAuditLogPartitionRepository
{
    public Task CreateMonthsAheadAsync(
        DateTimeOffset currentMonth,
        int monthsAhead) => Task.CompletedTask;

    public Task<int> DropMonthsBeforeAsync(DateTimeOffset keepFrom) => Task.FromResult(0);
}
