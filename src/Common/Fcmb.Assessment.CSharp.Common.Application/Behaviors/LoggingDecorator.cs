using Fcmb.Assessment.CSharp.Common.Application.Extensions;
using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Domain;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Fcmb.Assessment.CSharp.Common.Application.Behaviors;

internal static class LoggingDecorator
{
    internal sealed class RequestHandler<TRequest, TResponse>(
        IRequestHandler<TRequest, TResponse> innerHandler,
        ILogger<RequestHandler<TRequest, TResponse>> logger)
        : IRequestHandler<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TRequest request, CancellationToken cancellationToken)
        {
            string requestName = typeof(TRequest).GetOuterAndInnerName();

            logger.LogInformation("Processing {RequestName}", requestName);

            Result<TResponse> result = await innerHandler.Handle(request, cancellationToken);

            if (result.IsSuccess)
            {
                logger.LogInformation("Successfully completed {RequestName}", requestName);
            }
            else
            {
                using (LogContext.PushProperty("Error", result.Error, destructureObjects: true))
                {
                    logger.LogError("Completed {RequestName} with error {@Error}", requestName, result.Error);
                }
            }

            return result;
        }
    }

    internal sealed class RequestHandler<TRequest>(
        IRequestHandler<TRequest> innerHandler,
        ILogger<RequestHandler<TRequest>> logger)
        : IRequestHandler<TRequest>
        where TRequest : IRequest
    {
        public async Task<Result> Handle(TRequest request, CancellationToken cancellationToken)
        {
            string requestName = typeof(TRequest).GetOuterAndInnerName();

            logger.LogInformation("Processing {RequestName}", requestName);

            Result result = await innerHandler.Handle(request, cancellationToken);

            if (result.IsSuccess)
            {
                logger.LogInformation("Successfully completed {RequestName}", requestName);
            }
            else
            {
                using (LogContext.PushProperty("Error", result.Error, destructureObjects: true))
                {
                    logger.LogError("Completed {RequestName} with error {@Error}", requestName, result.Error);
                }
            }

            return result;
        }
    }
}
