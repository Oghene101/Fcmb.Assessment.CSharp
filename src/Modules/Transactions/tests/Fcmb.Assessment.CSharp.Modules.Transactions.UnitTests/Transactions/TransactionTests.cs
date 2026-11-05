using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Transactions;
using Fcmb.Assessment.CSharp.Modules.Transactions.UnitTests.Abstractions;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.UnitTests.Transactions;

public sealed class TransactionTests : BaseTest
{
    [Theory]
    [InlineData(TransactionType.IntraBank)]
    [InlineData(TransactionType.InterBank)]
    [InlineData(TransactionType.Airtime)]
    [InlineData(TransactionType.Data)]
    public void Create_ShouldReturnTransaction_WithTheGivenDetails(TransactionType transactionType)
    {
        string accountNumber = Faker.Finance.Account(10);
        const decimal amount = 50_000m;
        const decimal discountedAmount = 49_250m;
        const decimal rate = 1.5m;
        var customerId = Guid.NewGuid();

        Result<Transaction> result = Transaction.Create(
            transactionType,
            accountNumber,
            amount,
            discountedAmount,
            rate,
            customerId);

        result.IsSuccess.ShouldBeTrue();

        Transaction transaction = result.Value;

        transaction.TransactionType.ShouldBe(transactionType);
        transaction.AccountNumber.ShouldBe(accountNumber);
        transaction.Amount.ShouldBe(amount);
        transaction.DiscountedAmount.ShouldBe(discountedAmount);
        transaction.Rate.ShouldBe(rate);
    }

    [Fact]
    public void Create_ShouldStampTheAudit_AsCreatedByTheCustomer()
    {
        string accountNumber = Faker.Finance.Account(10);
        var customerId = Guid.NewGuid();

        Transaction transaction = Transaction.Create(
            TransactionType.IntraBank,
            accountNumber,
            10_000m,
            10_000m,
            0m,
            customerId);

        string owningCustomerId = customerId.ToString();

        transaction.Id.ShouldNotBe(Guid.Empty);
        transaction.CreatedBy.ShouldBe(owningCustomerId);
        transaction.DeletedAt.ShouldBeNull();
    }
}
