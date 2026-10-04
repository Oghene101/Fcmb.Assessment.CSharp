using System.Net;
using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Presentation;
using Fcmb.Assessment.CSharp.Modules.Users.Application.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static Fcmb.Assessment.CSharp.Modules.Users.Application.Contracts.Dtos.Users;
using static Fcmb.Assessment.CSharp.Modules.Users.Application.Users.SignUpUseCase;

namespace Fcmb.Assessment.CSharp.Modules.Users.Presentation.Users;

internal sealed class SignUpEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(Resources.Users + "/sign-up", async (
                SignUpRequest request,
                IRequestHandler<Command, SignUpResponse> handler,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var command = request.ToCommand();
                SignUpResponse result = await handler.Handle(command, cancellationToken);

                Uri uri = Helper.BuildUri(context, $"{Resources.Users}/{result.UserId}");
                return Results.Created(uri, ApiResponse.Success(result, statusCode: HttpStatusCode.Created));
            })
            .WithName("SignUpUser")
            .WithSummary("Register a new user profile")
            .WithDescription("""
                             Creates a new user record within the system. 
                             Validates registration parameters, ensures unique identity constraints,
                             and provisions authentication profiles.
                             """)
            .Produces<ApiResponse<SignUpResponse>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .MapToApiVersion(1)
            .WithTags(Tags.Users);
    }
}
