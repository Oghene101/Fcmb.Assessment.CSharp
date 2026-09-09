using System.Data;
using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Common.Application.Inbox;
using Fcmb.Assessment.CSharp.Common.Application.Outbox;
using Fcmb.Assessment.CSharp.Common.Infrastructure;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Inbox;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;
using Fcmb.Assessment.CSharp.Modules.Users.Application.Data;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Emails;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.PhoneNumbers;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Users;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Emails;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.PhoneNumbers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Database;

internal sealed class UnitOfWork(
    UsersDbContext context) : IUnitOfWork, IAsyncDisposable
{
    private IDbContextTransaction? _transaction;
    private IDbConnection DbConnection => context.Database.GetDbConnection();
    private IDbTransaction? DbTransaction => _transaction?.GetDbTransaction();

    # region Repositories

    #region OutboxMessages

    public IOutboxMessageRepository OutboxMessagesReadRepository =>
        field ??= new OutboxMessageRepository(Schemas.Users, DbConnection, DbTransaction);

    public IRepository<OutboxMessage> OutboxMessagesWriteRepository =>
        field ??= new Repository<OutboxMessage, UsersDbContext>(context);

    #endregion

    #region OutboxMessageConsumers

    public IOutboxMessageConsumerRepository OutboxMessageConsumersReadRepository =>
        field ??= new OutboxMessageConsumerRepository(Schemas.Users, DbConnection, DbTransaction);

    public IRepository<OutboxMessageConsumer> OutboxMessageConsumersWriteRepository =>
        field ??= new Repository<OutboxMessageConsumer, UsersDbContext>(context);

    #endregion

    #region DeadLetteredOutboxMessages

    public IRepository<DeadLetteredOutboxMessage> DeadLetteredOutboxMessagesWriteRepository =>
        field ??= new Repository<DeadLetteredOutboxMessage, UsersDbContext>(context);

    #endregion

    #region InboxMessages

    public IInboxMessageRepository InboxMessagesReadRepository =>
        field ??= new InboxMessageRepository(Schemas.Users, DbConnection, DbTransaction);

    public IRepository<InboxMessage> InboxMessagesWriteRepository =>
        field ??= new Repository<InboxMessage, UsersDbContext>(context);

    #endregion

    #region InboxMessageConsumers

    public IInboxMessageConsumerRepository InboxMessageConsumersReadRepository =>
        field ??= new InboxMessageConsumerRepository(Schemas.Users, DbConnection, DbTransaction);

    public IRepository<InboxMessageConsumer> InboxMessageConsumersWriteRepository =>
        field ??= new Repository<InboxMessageConsumer, UsersDbContext>(context);

    #endregion

    #region DeadLetteredInboxMessages

    public IRepository<DeadLetteredInboxMessage> DeadLetteredInboxMessagesWriteRepository =>
        field ??= new Repository<DeadLetteredInboxMessage, UsersDbContext>(context);

    #endregion

    #region Users

    public IRepository<User> UsersWriteRepository =>
        field ??= new Repository<User, UsersDbContext>(context);

    #endregion

    #region Emails

    public IEmailRepository EmailsReadRepository =>
        field ??= new EmailRepository(Schemas.Users, DbConnection, DbTransaction);

    public IRepository<Email> EmailsWriteRepository =>
        field ??= new Repository<Email, UsersDbContext>(context);

    #endregion

    #region PhoneNumbers

    public IPhoneNumberRepository PhoneNumbersReadRepository =>
        field ??= new PhoneNumberRepository(Schemas.Users, DbConnection, DbTransaction);

    public IRepository<PhoneNumber> PhoneNumbersWriteRepository =>
        field ??= new Repository<PhoneNumber, UsersDbContext>(context);

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
