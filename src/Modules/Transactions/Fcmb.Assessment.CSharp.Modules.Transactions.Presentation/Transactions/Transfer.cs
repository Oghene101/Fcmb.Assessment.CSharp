using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Presentation;
using Fcmb.Assessment.CSharp.Modules.Transactions.Application.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static
    Fcmb.Assessment.CSharp.Modules.Transactions.Application.Contracts.Dtos.Transactions;
using static Fcmb.Assessment.CSharp.Modules.Transactions.Application.Transactions.TransferUseCase;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.Presentation.Transactions;

internal sealed class TransferEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(Resources.Transactions + "/transfer", async (
                TransferRequest request,
                IRequestHandler<Command> handler,
                CancellationToken cancellationToken) =>
            {
                var command = request.ToCommand();
                await handler.Handle(command, cancellationToken);

                return ApiResponse.Success();
            })
            .WithName("InitiateTransfer")
            .WithSummary("Initiate a fund transfer")
            .WithDescription("""
                             Processes intra-bank or inter-bank account fund transfers.
                             Validates account parameters, verifies sufficient balances,
                             and executes the transaction across accounts.
                             """)
            .RequireAuthorization()
            .Produces<ApiResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .MapToApiVersion(1)
            .WithTags(Tags.Transactions);
    }
}
