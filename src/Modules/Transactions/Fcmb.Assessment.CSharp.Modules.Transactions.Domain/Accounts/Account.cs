using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Customers;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Accounts;

public sealed class Account : Entity
{
    private Account()
    {
    }

    public string AccountName { get; private set; }
    public AccountType AccountType { get; private set; }
    public string AccountNumber { get; private set; }
    public decimal AccountBalance { get; private set; }
    public Guid CustomerId { get; private set; }

    //Navigation props
    public Customer Customer { get; private set; } = null!;

    public static Result<Account> Create(
        string accountName,
        AccountType accountType,
        string accountNumber,
        Guid customerId)
    {
        var account = new Account
        {
            AccountName = accountName,
            AccountType = accountType,
            AccountNumber = accountNumber,
            AccountBalance = decimal.Zero,
            CustomerId = customerId
        };

        account.InitializeAudit(
            Guid.CreateVersion7(), 
            customerId.ToString());

        return account;
    }
}
