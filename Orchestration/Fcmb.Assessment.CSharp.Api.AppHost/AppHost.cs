using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Fcmb_Assessment_CSharp_Api>("fcmb-assessment-csharp-api");

await builder.Build().RunAsync();
