using System.Net;
using System.Net.Http.Json;
using Fcmb.Assessment.CSharp.IntegrationTests.Abstractions;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Customers;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Transactions;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Users;
using Microsoft.EntityFrameworkCore;
using static Fcmb.Assessment.CSharp.Modules.Transactions.Application.Contracts.Dtos.Transactions;
using SignUpRequest = Fcmb.Assessment.CSharp.Modules.Users.Application.Contracts.Dtos.Users.SignUpRequest;

namespace Fcmb.Assessment.CSharp.IntegrationTests.Transfer;

public sealed class TransferTests(
    IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task SignedUpUser_ShouldBeAbleTo_Transfer()
    {
        SignUpRequest signUp = CommandHelpers.NewSignUpRequest(UserType.Individual);

        await HttpClient.SignUpAsync(signUp, CancellationToken);

        Customer? customer = await Poller.WaitAsync(
            DeliveryTimeout,
            () => TransactionsDbContext.Customers
                .AsNoTracking()
                .SingleOrDefaultAsync(storedCustomer => storedCustomer.Email == signUp.Email, CancellationToken),
            CancellationToken);

        customer.ShouldNotBeNull();

        await AuthorizeAsync(signUp);

        TransferRequest transfer = CommandHelpers.NewTransferRequest();

        using HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("transactions/transfer", transfer, CancellationToken);

        Transaction? recorded = await TransactionsDbContext.Transactions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                transaction => transaction.AccountNumber == transfer.SourceAccount,
                CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        recorded.ShouldNotBeNull();
        recorded.Amount.ShouldBe(transfer.Amount);
    }
}
