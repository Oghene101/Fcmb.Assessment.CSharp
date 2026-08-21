using Fcmb.Assessment.CSharp.Common.Domain;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Transactions;

public sealed class Transaction : Entity
{
    private Transaction()
    {
    }

    public TransactionType TransactionType { get; private set; }
    public string AccountNumber { get; private set; }
    public decimal Amount { get; private set; }
    public decimal DiscountedAmount { get; private set; }
    public decimal Rate { get; private set; }

    public static Result<Transaction> Create(
        TransactionType transactionType,
        string accountNumber,
        decimal amount,
        decimal discountedAmount,
        decimal rate,
        Guid customerId)
    {
        var transaction = new Transaction
        {
            TransactionType = transactionType,
            AccountNumber = accountNumber,
            Amount = amount,
            DiscountedAmount = discountedAmount,
            Rate = rate
        };

        transaction.InitializeAudit(
            Guid.CreateVersion7(),
            customerId.ToString());

        return transaction;
    }
}
