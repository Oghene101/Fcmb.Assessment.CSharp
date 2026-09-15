using Fcmb.Assessment.CSharp.Common.Infrastructure.AuditLog;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Authentication;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Configurations;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;
using MassTransit;
using MassTransit.AzureServiceBusTransport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quartz;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure;

public static class InfrastructureConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure(
            IConfiguration configuration,
            string serviceName,
            Action<IQuartzBuilder, IConfiguration>[] moduleConfigureJobs,
            Action<IServiceBusBusFactoryConfigurator>[] moduleConfigureTopology,
            Action<IRegistrationConfigurator, string>[] moduleConfigureConsumers,
            string messageBrokerConnectionString)
        {
            #region Azure Service Bus Emulator Default Configs

            //These defaults are set because of Azure Service Bus Emulator constraints. Don't do this in prod.
            Defaults.DefaultMessageTimeToLive = TimeSpan.FromHours(1);
            Defaults.BasicMessageTimeToLive = TimeSpan.FromHours(1);
            Defaults.AutoDeleteOnIdle = TimeSpan.FromHours(1);

            #endregion

            services.AddOptionsInternal();

            services.AddAuthenticationInternal();

            services.AddSingleton(TimeProvider.System);

            services.TryAddSingleton<InsertOutboxMessagesInterceptor>();
            services.AddScoped<InsertAuditLogsInterceptor>();

            services.AddQuartz(quartz =>
            {
                foreach (Action<IQuartzBuilder, IConfiguration> configureQuartz in moduleConfigureJobs)
                {
                    configureQuartz(quartz, configuration);
                }
            });
            services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

            services.AddMassTransit(configurator =>
            {
                configurator.SetKebabCaseEndpointNameFormatter();

                string instanceId = serviceName.ToUpperInvariant().Replace('.', '-');
                foreach (Action<IRegistrationConfigurator, string> configureConsumers in moduleConfigureConsumers)
                {
                    configureConsumers(configurator, instanceId);
                }

                configurator.UsingAzureServiceBus((context, busFactoryConfigurator) =>
                {
                    busFactoryConfigurator.Host(messageBrokerConnectionString);

                    foreach (Action<IServiceBusBusFactoryConfigurator> configureTopology in moduleConfigureTopology)
                    {
                        configureTopology(busFactoryConfigurator);
                    }

                    busFactoryConfigurator.ConfigureEndpoints(context);
                });
            });

            return services;
        }
    }
}
