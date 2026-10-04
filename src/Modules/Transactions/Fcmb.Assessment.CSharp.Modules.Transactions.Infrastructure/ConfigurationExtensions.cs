using Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure.Inbox;
using Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure.Outbox;
using Microsoft.Extensions.DependencyInjection;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure;

internal static class ConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        internal IServiceCollection AddTransactionsOptions()
        {
            services.AddOptions<OutboxSettings>()
                .BindConfiguration(OutboxSettings.Path)
                .ValidateOnStart();

            services.AddOptions<InboxSettings>()
                .BindConfiguration(InboxSettings.Path)
                .ValidateOnStart();

            return services;
        }
    }
}
