using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using MassTransit;
using MassTransit.AzureServiceBusTransport.Configuration;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;

internal static class ServiceBusHostConfiguration
{
    // MassTransit 8 sends namespace management calls to the AMQP endpoint; the emulator serves them on this HTTP port.
    private const int EmulatorManagementPort = 5300;

    private const string EmulatorSetting = "UseDevelopmentEmulator=true";

    extension(IServiceBusBusFactoryConfigurator configurator)
    {
        public void UseServiceBusHost(string connectionString)
        {
            bool targetsEmulator = connectionString.Contains(EmulatorSetting, StringComparison.OrdinalIgnoreCase);

            if (!targetsEmulator)
            {
                configurator.Host(connectionString);

                return;
            }

            var connection = ServiceBusConnectionStringProperties.Parse(connectionString);

            string managementConnectionString = BuildEmulatorManagementConnectionString(connection);

            var hostSettings = new HostSettings
            {
                ServiceUri = new ServiceBusHostAddress(connection.Endpoint),
                ConnectionString = connectionString,
                ServiceBusAdministrationClient = new ServiceBusAdministrationClient(managementConnectionString)
            };

            configurator.Host(hostSettings);
        }
    }

    private static string BuildEmulatorManagementConnectionString(ServiceBusConnectionStringProperties connection)
    {
        var managementEndpoint = new UriBuilder(connection.Endpoint)
        {
            Port = EmulatorManagementPort
        };

        return $"Endpoint={managementEndpoint.Uri};SharedAccessKeyName={connection.SharedAccessKeyName};"
            + $"SharedAccessKey={connection.SharedAccessKey};{EmulatorSetting};";
    }
}
