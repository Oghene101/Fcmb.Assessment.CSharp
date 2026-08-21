using Fcmb.Assessment.CSharp.Api.Extensions;
using Fcmb.Assessment.CSharp.Api.Middleware;
using Fcmb.Assessment.CSharp.Common.Application;
using Fcmb.Assessment.CSharp.Common.Infrastructure;
using Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure;
using Scalar.AspNetCore;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
    loggerConfiguration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApiInternal();

builder.Services.AddApplication()
    .AddInfrastructure();

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

string[] summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        WeatherForecast[] forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
#pragma warning disable CA5394
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
#pragma warning restore CA5394
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

await app.RunAsync();

#pragma warning disable S3903
internal sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
#pragma warning restore S3903
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

//use aspire for dependencies
