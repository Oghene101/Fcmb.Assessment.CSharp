using Fcmb.Assessment.CSharp.Common.Application.Authorization;
using Fcmb.Assessment.CSharp.Common.Infrastructure.AuditLog;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Inbox;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Resilience;
using Fcmb.Assessment.CSharp.Common.Presentation.Extensions;
using Fcmb.Assessment.CSharp.Modules.Users.Application.Data;
using Fcmb.Assessment.CSharp.Modules.Users.Application.Integrations.KeyCloak;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Authorization;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Database;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Inbox;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Integrations.KeyCloak;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Outbox;
using Fcmb.Assessment.CSharp.Modules.Users.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Quartz;
using Refit;
using AssemblyReference = Fcmb.Assessment.CSharp.Modules.Users.Presentation.AssemblyReference;

namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure;

public static class UsersModule
{
    private const string Module = "Users";
    private const string OutboxSettingsPath = "Users:Outbox";
    private const string InboxSettingsPath = "Users:Inbox";

    extension(IServiceCollection services)
    {
        public IServiceCollection AddUsersModule(IConfiguration configuration)
        {
            services.AddUsersOptions();

            services.AddInfrastructure(configuration);

            services.AddOutbox<IUnitOfWork>(
                Module,
                OutboxSettingsPath,
                OutboxLanes.All,
                Application.AssemblyReference.Assembly);

            services.AddInbox<IUnitOfWork>(
                Module,
                InboxSettingsPath,
                InboxLanes.All,
                AssemblyReference.Assembly);

            services.AddEndpoints(AssemblyReference.Assembly);

            services.AddHttpClients(configuration);

            return services;
        }

        private void AddInfrastructure(IConfiguration configuration)
        {
            services.AddScoped<IClaimsProvider, ClaimsProvider>();

            services.AddDbContext<UsersDbContext>((sp, options) =>
                options
                    .UseSqlServer(
                        configuration.GetConnectionString("Database"),
                        sqlServerOptions => sqlServerOptions
                            .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Users))
                    .AddInterceptors(
                        new InsertOutboxMessagesInterceptor(OutboxLanes.Of),
                        sp.GetRequiredService<InsertAuditLogsInterceptor>()));

            services.AddScoped<IUnitOfWork, UnitOfWork>();
        }

        private void AddHttpClients(IConfiguration configuration)
        {
            string baseUrl = configuration[KeyCloakSettings.Path + ":BaseUrl"]!;

            services.AddTransient<KeycloakAuthHandler>();

            services.AddRefitGeneratedClient<IKeyCloakClient>()
                .ConfigureHttpClient(client => client.BaseAddress = new Uri(baseUrl))
                .AddHttpMessageHandler<KeycloakAuthHandler>()
                .ConfigureStandardPolicies();
        }

        public static void ConfigureJobs(
            IQuartzBuilder quartz,
            IConfiguration configuration)
        {
            OutboxSettings outbox = configuration
                .GetSection(OutboxSettingsPath)
                .Get<OutboxSettings>()!;

            InboxSettings inbox = configuration
                .GetSection(InboxSettingsPath)
                .Get<InboxSettings>()!;

            quartz.AddMessagePurgeJobs<IUnitOfWork>(Module, outbox, inbox);
        }

        public static void ConfigureConsumers(
            IRegistrationConfigurator registrationConfigurator,
            string instanceId)
        {
#pragma warning disable CA1303
            Console.WriteLine("No consumers for users module");
#pragma warning restore CA1303
        }

        public static void ConfigureTopology(IServiceBusBusFactoryConfigurator serviceBusBusFactoryConfigurator)
        {
            serviceBusBusFactoryConfigurator.Message<UserSignedUpIntegrationEvent>(m =>
                m.SetEntityName("domain-events-topic"));
        }
    }

    private static void ConfigureStandardPolicies(this IHttpClientBuilder builder)
    {
        builder.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 10
        });

        builder
            .AddStandardResilienceHandler()
            .Configure(StandardHttpPolicies.Configure);
    }
}
