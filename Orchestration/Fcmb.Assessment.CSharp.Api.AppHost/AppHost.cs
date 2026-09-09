using Aspire.Hosting.Azure;
using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<KeycloakResource> keycloak = builder.AddKeycloak("keycloak", port: 8080)
    .WithDataVolume();

IResourceBuilder<AzureServiceBusResource> serviceBus = builder.AddAzureServiceBus("azureservicebus")
    .RunAsEmulator(e => e.WithLifetime(ContainerLifetime.Persistent));

builder.AddProject<Fcmb_Assessment_CSharp_Api>("fcmb-assessment-csharp-api")
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithReference(serviceBus)
    .WaitFor(serviceBus);

await builder.Build().RunAsync();
