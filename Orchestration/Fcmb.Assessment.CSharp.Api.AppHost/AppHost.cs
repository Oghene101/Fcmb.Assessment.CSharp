using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<KeycloakResource> keycloak = builder.AddKeycloak("keycloak", port: 8080)
    .WithDataVolume();

builder.AddProject<Fcmb_Assessment_CSharp_Api>("fcmb-assessment-csharp-api")
    .WithReference(keycloak)
    .WaitFor(keycloak);

await builder.Build().RunAsync();
