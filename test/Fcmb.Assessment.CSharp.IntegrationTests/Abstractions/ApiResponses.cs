using System.Text.Json.Serialization;

namespace Fcmb.Assessment.CSharp.IntegrationTests.Abstractions;

internal sealed record ApiEnvelope<TData>(
    bool IsSuccess,
    string Message,
    TData Data);

internal sealed record AccessTokenResponse(
    [property: JsonPropertyName("access_token")]
    string AccessToken);
