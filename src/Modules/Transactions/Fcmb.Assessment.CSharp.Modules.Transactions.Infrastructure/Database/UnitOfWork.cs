using System.Data;
using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Common.Application.Outbox;
using Fcmb.Assessment.CSharp.Common.Infrastructure;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;
using Fcmb.Assessment.CSharp.Modules.Transactions.Application.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure.Database;

internal sealed class UnitOfWork(
    TransactionsDbContext context) : IUnitOfWork, IAsyncDisposable
{
    private IDbContextTransaction? _transaction;
    private IDbConnection DbConnection => context.Database.GetDbConnection();
    private IDbTransaction? DbTransaction => _transaction?.GetDbTransaction();

    # region Repositories

    #region OutboxMessages

    public IOutboxMessageRepository OutboxMessagesReadRepository =>
        field ??= new OutboxMessageRepository(Schemas.Transactions, DbConnection, DbTransaction);

    public IRepository<OutboxMessage> OutboxMessagesWriteRepository =>
        field ??= new Repository<OutboxMessage, TransactionsDbContext>(context);

    #endregion

    #region OutboxMessageConsumers

    public IOutboxMessageConsumerRepository OutboxMessageConsumersReadRepository =>
        field ??= new OutboxMessageConsumerRepository(Schemas.Transactions, DbConnection, DbTransaction);

    public IRepository<OutboxMessageConsumer> OutboxMessageConsumersWriteRepository =>
        field ??= new Repository<OutboxMessageConsumer, TransactionsDbContext>(context);

    #endregion

    #region DeadLetteredOutboxMessages

    public IRepository<DeadLetteredOutboxMessage> DeadLetteredOutboxMessagesWriteRepository =>
        field ??= new Repository<DeadLetteredOutboxMessage, TransactionsDbContext>(context);

    #endregion

    # endregion

    # region Transaction support (EF + Dapper can share the same transaction if needed)

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("There is already an active transaction.");
        }

        _transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) { throw new InvalidOperationException("There is no active transaction."); }

        await SaveChangesAsync(cancellationToken);
        await _transaction.CommitAsync(cancellationToken);
        await DisposeTransactionAsync();
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) { throw new InvalidOperationException("There is no active transaction."); }

        await _transaction.RollbackAsync(cancellationToken);
        await DisposeTransactionAsync();
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);


    public async ValueTask DisposeAsync()
    {
        await DisposeTransactionAsync();
    }

    #endregion
}
