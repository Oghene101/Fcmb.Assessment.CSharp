using Fcmb.Assessment.CSharp.Api.Extensions;
using Fcmb.Assessment.CSharp.Api.Middleware;
using Fcmb.Assessment.CSharp.Api.OpenTelemetry;
using Fcmb.Assessment.CSharp.Common.Application;
using Fcmb.Assessment.CSharp.Common.Infrastructure;
using Fcmb.Assessment.CSharp.Common.Presentation.Extensions;
using Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure;
using Scalar.AspNetCore;
using Serilog;
using AssemblyReference = Fcmb.Assessment.CSharp.Modules.Users.Application.AssemblyReference;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
    loggerConfiguration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApiInternal();

builder.Services.AddApplication([
    AssemblyReference.Assembly,
    Fcmb.Assessment.CSharp.Modules.Transactions.Application.AssemblyReference.Assembly
]);

builder.Configuration.AddModuleConfiguration(["users", "transactions"]);

string messageBrokerConnectionString = builder.Configuration.GetConnectionString("azureservicebus")!;

builder.Services.AddInfrastructure(
    builder.Configuration,
    DiagnosticsConfig.ServiceName,
    [
        UsersModule.ConfigureJobs,
    ],
    [
        UsersModule.ConfigureTopology,
        TransactionsModule.ConfigureTopology
    ],
    [
        UsersModule.ConfigureConsumers,
        TransactionsModule.ConfigureConsumers
    ],
    messageBrokerConnectionString);

builder.Services.AddUsersModule(builder.Configuration);
builder.Services.AddTransactionsModule(builder.Configuration);

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().WithDocumentPerVersion();

    app.MapScalarApiReference(options =>
    {
        options.WithTitle("FCMB Assessment - C#")
            .WithTheme(ScalarTheme.Laserwave)
            .EnableDarkMode()
            .SortTagsAlphabetically()
            .SortOperationsByMethod()
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
#pragma warning disable S3878
            .AddPreferredSecuritySchemes("Bearer");
#pragma warning restore S3878

        options.AddDocument("v1", "FCMB Assessment - C#", "/openapi/v1.json");
    }).DisableRateLimiting();

    app.ApplyMigrations();
}

app.UseHttpsRedirection();

app.MapEndpoints();

await app.RunAsync();

//create an external requests table
//configure standard policies to httpclients
//integrate service bus
