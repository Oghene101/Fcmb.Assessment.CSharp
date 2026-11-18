using Aspire.Hosting.Azure;
using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<SqlServerServerResource> sqlServer = builder.AddSqlServer("sqlserver")
    .WithDataVolume();

// The resource name is the connection-string key both modules read; the second argument is the database name.
IResourceBuilder<SqlServerDatabaseResource> database = sqlServer.AddDatabase("Database", "fcmb_assessment_csharp");

IResourceBuilder<KeycloakResource> keycloak = builder.AddKeycloak("keycloak", port: 8080)
    .WithDataVolume();

IResourceBuilder<AzureServiceBusResource> serviceBus =
    builder.AddAzureServiceBus("azureservicebus")
        .RunAsEmulator(resourceBuilder =>
            resourceBuilder
                .WithLifetime(ContainerLifetime.Persistent)
                .WithHostPort(5672)
                .WithHttpEndpoint(
                    port: 5300,
                    targetPort: 5300,
                    name: "emulatorhealth"));

IResourceBuilder<ProjectResource> api = builder.AddProject<Fcmb_Assessment_CSharp_Api>("fcmb-assessment-csharp-api")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithReference(serviceBus)
    .WaitFor(serviceBus);

builder.AddProject<Fcmb_Assessment_CSharp_Gateway>("fcmb-assessment-csharp-gateway")
    .WithReference(api)
    .WaitFor(api)
    .WithReference(keycloak)
    .WaitFor(keycloak);

await builder.Build().RunAsync();
