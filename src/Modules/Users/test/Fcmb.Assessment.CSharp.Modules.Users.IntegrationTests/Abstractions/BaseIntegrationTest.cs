using System.Net.Http.Json;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Database;
using MassTransit.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Fcmb.Assessment.CSharp.Modules.Users.IntegrationTests.Abstractions;

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

        Harness = factory.Services.GetTestHarness();
    }

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    protected HttpClient HttpClient { get; }

    protected ITestHarness Harness { get; }

    protected IServiceProvider ServiceProvider => _scope.ServiceProvider;

    protected UsersDbContext DbContext => ServiceProvider.GetRequiredService<UsersDbContext>();

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

    protected async Task<TMessage?> WaitForPublishedAsync<TMessage>(Func<TMessage, bool> isExpected)
        where TMessage : class
    {
        using var deliveryWindow = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);

        deliveryWindow.CancelAfter(DeliveryTimeout);

        bool arrived = await Harness.Published.Any<TMessage>(
            candidate => Matches(candidate, isExpected),
            deliveryWindow.Token);

        if (!arrived)
        {
            return null;
        }

        TMessage[] published = PublishedSoFar(isExpected);

        return published[0];
    }

    protected TMessage[] PublishedSoFar<TMessage>(Func<TMessage, bool> isExpected)
        where TMessage : class
    {
        var noWaiting = new CancellationToken(canceled: true);

        IEnumerable<IPublishedMessage<TMessage>> published = Harness.Published.Select<TMessage>(
            candidate => Matches(candidate, isExpected),
            noWaiting);

        return [.. published.Select(message => message.Message).OfType<TMessage>()];
    }

    private static bool Matches<TMessage>(
        IPublishedMessage<TMessage> candidate,
        Func<TMessage, bool> isExpected)
        where TMessage : class
    {
        TMessage? message = candidate.Message;

        if (message is null)
        {
            return false;
        }

        return isExpected(message);
    }

    public async ValueTask DisposeAsync()
    {
        HttpClient.Dispose();

        await _scope.DisposeAsync();

        GC.SuppressFinalize(this);
    }
}
