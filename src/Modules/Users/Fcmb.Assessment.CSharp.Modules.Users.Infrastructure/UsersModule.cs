using Fcmb.Assessment.CSharp.Common.Application.Authorization;
using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Infrastructure.AuditLog;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;
using Fcmb.Assessment.CSharp.Common.Presentation.Extensions;
using Fcmb.Assessment.CSharp.Modules.Users.Application.Data;
using Fcmb.Assessment.CSharp.Modules.Users.Application.Integrations.KeyCloak;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Authorization;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Configurations;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Database;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Inbox;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Integrations;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Outbox;
using Fcmb.Assessment.CSharp.Modules.Users.IntegrationEvents;
using Fcmb.Assessment.CSharp.Modules.Users.Presentation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Refit;

namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure;

public static class UsersModule
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddUsersModule(IConfiguration configuration)
        {
            services.AddUsersOptions();

            services.AddDomainEventHandlers();

            services.AddIntegrationEventHandlers();

            services.AddInfrastructure(configuration);

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
                        sp.GetRequiredService<InsertOutboxMessagesInterceptor>(),
                        sp.GetRequiredService<InsertAuditLogsInterceptor>()));

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            //services.ConfigureOptions<ConfigureProcessOutboxJob>();
        }

        private void AddDomainEventHandlers()
        {
            Type[] domainEventHandlerServiceTypes = services
                .Where(sd => sd.ServiceType.IsGenericType &&
                             sd.ServiceType.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>) &&
                             sd.ImplementationType != null &&
                             sd.ImplementationType.Assembly == Application.AssemblyReference.Assembly)
                .Select(sd => sd.ServiceType)
                .Distinct()
                .ToArray();

            foreach (Type serviceType in domainEventHandlerServiceTypes)
            {
                Type domainEvent = serviceType
                    .GetGenericArguments()
                    .Single();

                Type closedIdempotentHandler = typeof(IdempotentDomainEventHandler<>).MakeGenericType(domainEvent);

                services.Decorate(serviceType, closedIdempotentHandler);
            }
        }

        private void AddIntegrationEventHandlers()
        {
            Type[] integrationEventHandlerServiceTypes = services
                .Where(sd => sd.ServiceType.IsGenericType &&
                             sd.ServiceType.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>) &&
                             sd.ImplementationType != null &&
                             sd.ImplementationType.Assembly == AssemblyReference.Assembly)
                .Select(sd => sd.ServiceType)
                .Distinct()
                .ToArray();

            foreach (Type serviceType in integrationEventHandlerServiceTypes)
            {
                Type integrationEvent = serviceType
                    .GetGenericArguments()
                    .Single();

                Type closedIdempotentHandler =
                    typeof(IdempotentIntegrationEventHandler<>).MakeGenericType(integrationEvent);

                services.Decorate(serviceType, closedIdempotentHandler);
            }
        }

        private void AddHttpClients(IConfiguration configuration)
        {
            string baseUrl = configuration[KeyCloakSettings.Path + ":BaseUrl"]!;

            services.AddTransient<KeycloakAuthHandler>();

            services.AddRefitGeneratedClient<IKeyCloakClient>()
                .ConfigureHttpClient(client => client.BaseAddress = new Uri(baseUrl))
                .AddHttpMessageHandler<KeycloakAuthHandler>();
        }

        public static void ConfigureJobs(
            IQuartzBuilder quartz,
            IConfiguration configuration)
        {
            OutboxSettings outbox = configuration
                .GetSection(OutboxSettings.Path)
                .Get<OutboxSettings>()!;

            quartz.AddProcessOutboxJob(outbox);
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
}
