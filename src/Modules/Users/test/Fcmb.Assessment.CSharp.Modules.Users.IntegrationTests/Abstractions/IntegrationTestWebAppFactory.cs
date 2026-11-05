using Fcmb.Assessment.CSharp.Modules.Users.Application.Integrations.KeyCloak;
using MassTransit;
using MassTransit.Monitoring;
using MassTransit.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Refit;
using Testcontainers.Keycloak;
using Testcontainers.MsSql;

namespace Fcmb.Assessment.CSharp.Modules.Users.IntegrationTests.Abstractions;

public sealed class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DatabaseName = "fcmb_assessment_csharp";

    // Nothing listens on these: anything that slipped past the in-memory bus or the disabled telemetry fails locally.
    private const string UnreachableServiceBus =
        "Endpoint=sb://127.0.0.1:1;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=unused;"
        + "UseDevelopmentEmulator=true;";

    private const string LoopbackTelemetryCollector = "http://127.0.0.1:4317";

    private static readonly string RealmExportPath =
        Path.Combine(AppContext.BaseDirectory, KeycloakRealm.ExportFileName);

    private static readonly TimeSpan BusStartTimeout = TimeSpan.FromSeconds(30);

    // Long enough to outlast a whole run; each test bounds its own waits instead.
    private static readonly TimeSpan HarnessTimeout = TimeSpan.FromMinutes(30);

    private readonly MsSqlContainer _databaseContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private readonly KeycloakContainer _keycloakContainer = new KeycloakBuilder("quay.io/keycloak/keycloak:26.4.0")
        .WithResourceMapping(
            new FileInfo(RealmExportPath),
            new FileInfo(KeycloakRealm.ImportPath))
        .WithCommand("--import-realm")
        .Build();

    private readonly Lock _busLock = new();

    private IBusControl? _bus;

    private string _keycloakBaseUrl = string.Empty;

    public Uri KeycloakTokenUri { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        Task databaseStarted = _databaseContainer.StartAsync(cancellationToken);
        Task keycloakStarted = _keycloakContainer.StartAsync(cancellationToken);

        await Task.WhenAll(databaseStarted, keycloakStarted);

        _keycloakBaseUrl = _keycloakContainer.GetBaseAddress().TrimEnd('/');

        KeycloakTokenUri = new Uri($"{RealmUrl}/protocol/openid-connect/token");
    }

    // MassTransit waives its licence only for a bus built from a test runner's call stack. The host starts on
    // WebApplicationFactory's own entry-point thread, so the bus is built here, from a test's constructor.
    public void StartBus()
    {
        lock (_busLock)
        {
            if (_bus is null)
            {
                IBusControl bus = Services.GetRequiredService<IBusControl>();

                bus.Start(BusStartTimeout);

                _bus = bus;
            }
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await StopBusAsync();

        await base.DisposeAsync();

        await _keycloakContainer.DisposeAsync();
        await _databaseContainer.DisposeAsync();
    }

    private async Task StopBusAsync()
    {
        if (_bus is null)
        {
            return;
        }

        await _bus.StopAsync(CancellationToken.None);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Dictionary<string, string?> testSettings = BuildTestSettings();

        foreach ((string key, string? value) in testSettings)
        {
            builder.UseSetting(key, value);
        }

        // Program layers modules.*.json (and any local modules.*.Development.json) over command-line settings,
        // so the same settings are applied once more when the host is built, after every file source.
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(testSettings));

        // The Serilog OTLP sink reads the endpoint from the process environment rather than configuration.
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", LoopbackTelemetryCollector);

        var keycloakBaseUri = new Uri(_keycloakBaseUrl);

        string keycloakClientName = UniqueName.ForType<IKeyCloakClient>();

        builder.ConfigureTestServices(services =>
        {
            services.AddMassTransitTestHarness();

            services.Configure<TestHarnessOptions>(options =>
            {
                options.TestTimeout = HarnessTimeout;
                options.TestInactivityTimeout = HarnessTimeout;
            });

            services.RemoveHostedService<MassTransitHostedService>();

            ServiceDescriptor[] busHealthChecks =
            [
                .. services.Where(descriptor =>
                    descriptor.ImplementationType == typeof(ConfigureBusHealthCheckServiceOptions))
            ];

            foreach (ServiceDescriptor busHealthCheck in busHealthChecks)
            {
                services.Remove(busHealthCheck);
            }

            // UsersModule captures KeyCloak:BaseUrl while registering, before the test settings above apply.
            services.Configure<HttpClientFactoryOptions>(keycloakClientName, options =>
            {
                options.HttpClientActions.Clear();
                options.HttpClientActions.Add(client => client.BaseAddress = keycloakBaseUri);
            });
        });
    }

    private string RealmUrl => $"{_keycloakBaseUrl}/realms/{KeycloakRealm.Name}";

    private Dictionary<string, string?> BuildTestSettings()
    {
        string databaseConnectionString = BuildDatabaseConnectionString();

        return new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = databaseConnectionString,
            ["ConnectionStrings:azureservicebus"] = UnreachableServiceBus,
            ["Authentication:Audience"] = KeycloakRealm.PublicClientId,
            ["Authentication:MetadataAddress"] = $"{RealmUrl}/.well-known/openid-configuration",
            ["Authentication:TokenValidationParameters:ValidIssuers:0"] = RealmUrl,
            ["Authentication:RequireHttpsMetadata"] = "false",
            ["KeyCloak:BaseUrl"] = _keycloakBaseUrl,
            ["KeyCloak:ClientId"] = KeycloakRealm.AdminClientId,
            ["KeyCloak:ClientSecret"] = KeycloakRealm.AdminClientSecret,
            ["KeyCloak:TokenUrl"] = $"{RealmUrl}/protocol/openid-connect/token",
            ["KeyCloak:AuthorizationUrl"] = $"{RealmUrl}/protocol/openid-connect/auth",
            ["KeyCloak:HealthUrl"] = $"{RealmUrl}/.well-known/openid-configuration",
            ["OTEL_SDK_DISABLED"] = "true",
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = LoopbackTelemetryCollector
        };
    }

    private string BuildDatabaseConnectionString()
    {
        string masterConnectionString = _databaseContainer.GetConnectionString();

        var connectionString = new SqlConnectionStringBuilder(masterConnectionString)
        {
            InitialCatalog = DatabaseName
        };

        return connectionString.ConnectionString;
    }
}
