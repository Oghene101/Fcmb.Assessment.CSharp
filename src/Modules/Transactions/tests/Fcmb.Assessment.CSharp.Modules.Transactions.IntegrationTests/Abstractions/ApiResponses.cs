using System.Text.Json.Serialization;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.IntegrationTests.Abstractions;

internal sealed record ApiEnvelope(
    bool IsSuccess,
    string Message);

internal sealed record AccessTokenResponse(
    [property: JsonPropertyName("access_token")]
    string AccessToken);
