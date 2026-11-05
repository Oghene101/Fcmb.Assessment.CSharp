using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Accounts;
using Fcmb.Assessment.CSharp.Modules.Transactions.UnitTests.Abstractions;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.UnitTests.Accounts;

public sealed class AccountTests : BaseTest
{
    [Theory]
    [InlineData(AccountType.Savings)]
    [InlineData(AccountType.Current)]
    public void Create_ShouldReturnAccount_WithTheGivenDetails(AccountType accountType)
    {
        string accountName = Faker.Finance.AccountName();
        string accountNumber = Faker.Finance.Account(10);
        var customerId = Guid.NewGuid();

        Result<Account> result = Account.Create(accountName, accountType, accountNumber, customerId);

        result.IsSuccess.ShouldBeTrue();

        Account account = result.Value;

        account.AccountName.ShouldBe(accountName);
        account.AccountType.ShouldBe(accountType);
        account.AccountNumber.ShouldBe(accountNumber);
        account.CustomerId.ShouldBe(customerId);
    }

    [Fact]
    public void Create_ShouldOpenTheAccount_WithAZeroBalance()
    {
        Account account = CreateAccount(Guid.NewGuid());

        account.AccountBalance.ShouldBe(decimal.Zero);
    }

    [Fact]
    public void Create_ShouldStampTheAudit_AsCreatedByTheOwningCustomer()
    {
        var customerId = Guid.NewGuid();

        Account account = CreateAccount(customerId);

        string owningCustomerId = customerId.ToString();

        account.Id.ShouldNotBe(Guid.Empty);
        account.Id.ShouldNotBe(customerId);
        account.CreatedBy.ShouldBe(owningCustomerId);
        account.DeletedAt.ShouldBeNull();
    }

    private static Account CreateAccount(Guid customerId)
    {
        string accountName = Faker.Finance.AccountName();
        string accountNumber = Faker.Finance.Account(10);

        return Account.Create(accountName, AccountType.Savings, accountNumber, customerId);
    }
}
