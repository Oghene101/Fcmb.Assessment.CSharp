using System.Net.Http.Headers;
using System.Net.Http.Json;
using Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SignUpRequest = Fcmb.Assessment.CSharp.Modules.Users.Application.Contracts.Dtos.Users.SignUpRequest;

namespace Fcmb.Assessment.CSharp.IntegrationTests.Abstractions;

[Collection(nameof(IntegrationTestCollection))]
public abstract class BaseIntegrationTest : IAsyncDisposable
{
    private static readonly Uri ApiBaseAddress = new("http://localhost/api/v1/");

    private readonly IntegrationTestWebAppFactory _factory;

    private readonly AsyncServiceScope _scope;

    protected static readonly TimeSpan DeliveryTimeout = TimeSpan.FromSeconds(30);

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;

        factory.StartBus();

        _scope = factory.Services.CreateAsyncScope();

        HttpClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = ApiBaseAddress
        });
    }

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    protected HttpClient HttpClient { get; }

    protected IServiceProvider ServiceProvider => _scope.ServiceProvider;

    protected TransactionsDbContext TransactionsDbContext =>
        ServiceProvider.GetRequiredService<TransactionsDbContext>();

    protected async Task<string> GetAccessTokenAsync(
        string email,
        string password)
    {
        var passwordGrant = new Dictionary<string, string>
        {
            ["client_id"] = KeycloakRealm.PublicClientId,
            ["grant_type"] = "password",
            ["scope"] = "openid",
            ["username"] = email,
            ["password"] = password
        };

        using var tokenClient = new HttpClient();
        using var tokenRequest = new FormUrlEncodedContent(passwordGrant);

        using HttpResponseMessage tokenResponse =
            await tokenClient.PostAsync(_factory.KeycloakTokenUri, tokenRequest, CancellationToken);

        tokenResponse.EnsureSuccessStatusCode();

        AccessTokenResponse? accessToken =
            await tokenResponse.Content.ReadFromJsonAsync<AccessTokenResponse>(CancellationToken);

        return accessToken?.AccessToken ?? throw new InvalidOperationException("Keycloak issued no access token.");
    }

    protected async Task AuthorizeAsync(SignUpRequest signedUp)
    {
        string accessToken = await GetAccessTokenAsync(signedUp.Email, signedUp.Password);

        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public async ValueTask DisposeAsync()
    {
        HttpClient.Dispose();

        await _scope.DisposeAsync();

        GC.SuppressFinalize(this);
    }
}
