using System.Diagnostics.CodeAnalysis;
using Fcmb.Assessment.CSharp.Common.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Fcmb.Assessment.CSharp.Api.Middleware;

#pragma warning disable CA1873
internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    [RequiresDynamicCode("Calls Microsoft.AspNetCore.Http.HttpResponseJsonExtensions.WriteAsJsonAsync<TValue>(TValue, CancellationToken)")]
    [RequiresUnreferencedCode("Calls Microsoft.AspNetCore.Http.HttpResponseJsonExtensions.WriteAsJsonAsync<TValue>(TValue, CancellationToken)")]
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException && exception is not ApiException)
        {
            logger.LogError(exception, "Unhandled exception occurred while processing request");
        }
        else
        {
            switch (exception)
            {
                case ValidationException validationException:
                    logger.LogInformation("""
                                          Validation Errors: {Errors}
                                          """, validationException.Errors);

                    break;
                case ApiException apiException:
                    logger.LogInformation("""
                                          {Title} 
                                          {Errors}
                                          """, apiException.Title, apiException.Extensions);
                    break;
            }
        }

        ProblemDetails problemDetails = exception switch
        {
            ValidationException validationException => new ValidationProblemDetails
            {
                Type =
                    $"https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/{StatusCodes.Status400BadRequest}",
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = "One or more validation errors occurred",
                Errors = validationException.Errors,
                Instance = httpContext.Request.Path
            },
            ApiException apiException => new ProblemDetails
            {
                Type = $"https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/{apiException.StatusCode}",
                Title = apiException.Title,
                Status = apiException.StatusCode,
                Detail = apiException.Message,
                Extensions = apiException.Extensions,
                Instance = httpContext.Request.Path,
            },
            _ => new ProblemDetails
            {
                Type =
                    $"https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/{StatusCodes.Status500InternalServerError}",
                Title = "Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unexpected error occurred",
                Instance = httpContext.Request.Path,
            }
        };

        httpContext.Response.Clear();
        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        if (problemDetails is ValidationProblemDetails vpd)
        {
            await httpContext.Response.WriteAsJsonAsync(vpd, cancellationToken);
        }
        else
        {
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        }

        return true;
    }
}
#pragma warning restore CA1873
