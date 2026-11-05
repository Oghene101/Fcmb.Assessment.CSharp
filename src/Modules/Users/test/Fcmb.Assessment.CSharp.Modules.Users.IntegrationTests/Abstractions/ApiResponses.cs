using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Fcmb.Assessment.CSharp.Modules.Users.IntegrationTests.Abstractions;

internal sealed record ApiEnvelope<TData>(
    bool IsSuccess,
    string Message,
    TData Data);

internal sealed record AccessTokenResponse(
    [property: JsonPropertyName("access_token")]
    string AccessToken);

internal static class ProblemDetailsExtensions
{
    internal static string? ErrorCode(this ProblemDetails problemDetails)
    {
        bool hasError = problemDetails.Extensions.TryGetValue("error", out object? error);

        if (hasError && error is JsonElement errorElement)
        {
            JsonElement code = errorElement.GetProperty("code");

            return code.GetString();
        }

        return null;
    }
}
