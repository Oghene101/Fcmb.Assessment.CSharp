using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Transactions;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.Application.Data;

public interface IUnitOfWork : ICommonUnitOfWork
{
    IRepository<Transaction> TransactionsWriteRepository { get; }
}
