using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Configurations;
using Microsoft.Extensions.Options;

namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Integrations;

internal sealed class KeycloakAuthHandler(
    IHttpClientFactory httpClientFactory,
    IOptions<KeyCloakSettings> keyCloak) : DelegatingHandler
{
    private readonly KeyCloakSettings _keyCloak = keyCloak.Value;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string accessToken = await GetAdminTokenAsync(cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetAdminTokenAsync(CancellationToken cancellationToken)
    {
        HttpClient authClient = httpClientFactory.CreateClient("KeycloakAuthTokenClient");

        var payload = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", _keyCloak.ClientId },
            { "client_secret", _keyCloak.ClientSecret }
        };

        using var content = new FormUrlEncodedContent(payload);

        HttpResponseMessage response =
            await authClient.PostAsync(_keyCloak.TokenUrl, content, cancellationToken);

        response.EnsureSuccessStatusCode();

        TokenResponse? tokenResponse =
            await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);
        return tokenResponse?.AccessToken ??
               throw new Exception("Failed to retrieve valid token payload from Keycloak server.");
    }

    internal sealed record TokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; init; }
    }
}
