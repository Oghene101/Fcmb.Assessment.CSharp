using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Fcmb.Assessment.CSharp.Common.Application.Exceptions;
using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Modules.Transactions.Application.Extensions;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Transactions;
using Fcmb.Assessment.CSharp.Modules.Transactions.IntegrationTests.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Fcmb.Assessment.CSharp.Modules.Transactions.Application.Contracts.Dtos.Transactions;
using static Fcmb.Assessment.CSharp.Modules.Transactions.Application.Transactions.TransferUseCase;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.IntegrationTests.Transactions;

public sealed class TransferTests(
    IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string TransferRoute = "transactions/transfer";

    public static readonly TheoryData<string, string, decimal, string> InvalidTransfers = new()
    {
        { "", "0123456789", 5_000m, nameof(TransferRequest.SourceAccount) },
        { "012345678", "0123456789", 5_000m, nameof(TransferRequest.SourceAccount) },
        { "01234567AB", "0123456789", 5_000m, nameof(TransferRequest.SourceAccount) },
        { "0123456789", "", 5_000m, nameof(TransferRequest.DestinationAccount) },
        { "0123456789", "98765432101", 5_000m, nameof(TransferRequest.DestinationAccount) },
        { "0123456789", "98765-4321", 5_000m, nameof(TransferRequest.DestinationAccount) },
        { "0123456789", "0123456789", 5_000m, nameof(TransferRequest.DestinationAccount) },
        { "0123456789", "9876543210", 0m, nameof(TransferRequest.Amount) },
        { "0123456789", "9876543210", -1m, nameof(TransferRequest.Amount) }
    };

    [Fact]
    public async Task Should_ReturnUnauthorized_WhenNoAccessTokenIsSent()
    {
        TransferRequest request = CommandHelpers.NewTransferRequest();

        using HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync(TransferRoute, request, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_WhenTheAccessTokenWasNotIssuedByTheIdentityProvider()
    {
        TransferRequest request = CommandHelpers.NewTransferRequest();
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

        using HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync(TransferRoute, request, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(InvalidTransfers))]
    public async Task Should_ReturnBadRequest_WhenTheTransferIsInvalid(
        string sourceAccount,
        string destinationAccount,
        decimal amount,
        string invalidField)
    {
        await AuthorizeAsANewlySignedUpUserAsync();

        var request = new TransferRequest(TransferType.IntraBank, sourceAccount, destinationAccount, amount);

        using HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync(TransferRoute, request, CancellationToken);

        HttpValidationProblemDetails? problem =
            await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problem.ShouldNotBeNull();
        problem.Errors.ShouldContainKey(invalidField);
    }

    [Fact]
    public async Task Should_ReturnOk_WhenTheTransferIsValid()
    {
        await AuthorizeAsANewlySignedUpUserAsync();

        TransferRequest request = CommandHelpers.NewTransferRequest();

        using HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync(TransferRoute, request, CancellationToken);

        ApiEnvelope? completed = await response.Content.ReadFromJsonAsync<ApiEnvelope>(CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        completed.ShouldNotBeNull();
        completed.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(TransferType.IntraBank, TransactionType.IntraBank)]
    [InlineData(TransferType.InterBank, TransactionType.InterBank)]
    public async Task Should_RecordTheTransaction_AgainstTheSourceAccount_WhenTheTransferIsValid(
        TransferType transferType,
        TransactionType expectedTransactionType)
    {
        await AuthorizeAsANewlySignedUpUserAsync();

        TransferRequest request = CommandHelpers.NewTransferRequest(transferType);

        using HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync(TransferRoute, request, CancellationToken);

        Transaction? recorded = await DbContext.Transactions
            .AsNoTracking()
            .SingleOrDefaultAsync(transaction => transaction.AccountNumber == request.SourceAccount, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        recorded.ShouldNotBeNull();
        recorded.TransactionType.ShouldBe(expectedTransactionType);
        recorded.Amount.ShouldBe(request.Amount);
    }

    [Fact]
    public async Task Should_RecordTheTransaction_WhenTheTransferCommandIsHandled()
    {
        TransferRequest request = CommandHelpers.NewTransferRequest();
        var command = request.ToCommand();
        IRequestHandler<Command> handler = ServiceProvider.GetRequiredService<IRequestHandler<Command>>();

        Result result = await handler.Handle(command, CancellationToken);

        bool recorded = await DbContext.Transactions
            .AnyAsync(transaction => transaction.AccountNumber == request.SourceAccount, CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        recorded.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_RejectTheTransferCommand_AndRecordNothing_WhenItIsInvalid()
    {
        TransferRequest validRequest = CommandHelpers.NewTransferRequest();
        TransferRequest request = validRequest with { DestinationAccount = validRequest.SourceAccount };
        var command = request.ToCommand();
        IRequestHandler<Command> handler = ServiceProvider.GetRequiredService<IRequestHandler<Command>>();

        ValidationException exception =
            await Should.ThrowAsync<ValidationException>(() => handler.Handle(command, CancellationToken));

        bool recorded = await DbContext.Transactions
            .AnyAsync(transaction => transaction.AccountNumber == request.SourceAccount, CancellationToken);

        exception.Errors.ShouldContainKey(nameof(Command.DestinationAccount));
        recorded.ShouldBeFalse();
    }
}
