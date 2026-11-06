using Fcmb.Assessment.CSharp.Common.Infrastructure.AuditLog;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Inbox;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;
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
using Quartz;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure;

public static class TransactionsModule
{
    private const string Module = "Transactions";
    private const string OutboxSettingsPath = "Transactions:Outbox";
    private const string InboxSettingsPath = "Transactions:Inbox";
    private const string AuditLogSettingsPath = "Transactions:AuditLogs";

    extension(IServiceCollection services)
    {
        public IServiceCollection AddTransactionsModule(IConfiguration configuration)
        {
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

            services.AddAuditLogPartitions<IUnitOfWork>(Module, AuditLogSettingsPath);

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
                    .AddInterceptors(
                        new InsertOutboxMessagesInterceptor(OutboxLanes.Of),
                        sp.GetRequiredService<InsertAuditLogsInterceptor>()));

            services.AddScoped<IUnitOfWork, UnitOfWork>();
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

            AuditLogSettings auditLogs = configuration
                .GetSection(AuditLogSettingsPath)
                .Get<AuditLogSettings>()!;

            quartz.AddAuditLogPartitionsJob<IUnitOfWork>(Module, auditLogs);
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
