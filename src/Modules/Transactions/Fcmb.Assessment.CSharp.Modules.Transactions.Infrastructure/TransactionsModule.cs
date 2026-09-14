using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Infrastructure.AuditLog;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;
using Fcmb.Assessment.CSharp.Common.Presentation.Extensions;
using Fcmb.Assessment.CSharp.Modules.Transactions.Application.Data;
using Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure.Database;
using Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure.Inbox;
using Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure.Outbox;
using Fcmb.Assessment.CSharp.Modules.Transactions.Presentation;
using Fcmb.Assessment.CSharp.Modules.Users.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure;

public static class TransactionsModule
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddTransactionsModule(IConfiguration configuration)
        {
            services.AddInfrastructure(configuration);

            services.AddEndpoints(AssemblyReference.Assembly);

            return services;
        }

        private void AddInfrastructure(IConfiguration configuration)
        {
            services.AddDbContext<TransactionsDbContext>((sp, options) =>
                options
                    .UseSqlServer(
                        configuration.GetConnectionString("Database"),
                        sqlServerOptions => sqlServerOptions
                            .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Transactions))
                    .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>(),
                        sp.GetRequiredService<InsertAuditLogsInterceptor>()));

            services.AddScoped<IUnitOfWork, UnitOfWork>();
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

        public static void ConfigureConsumers(
            IRegistrationConfigurator registrationConfigurator,
            string instanceId)
        {
            registrationConfigurator.AddConsumer<IntegrationEventConsumer<UserSignedUpIntegrationEvent>>()
                .Endpoint(c => c.InstanceId = instanceId);
        }

        public static void ConfigureTopology(IServiceBusBusFactoryConfigurator serviceBusBusFactoryConfigurator)
        {
            serviceBusBusFactoryConfigurator.Message<UserSignedUpIntegrationEvent>(m =>
                m.SetEntityName("domain-events-topic"));
        }
    }
}
