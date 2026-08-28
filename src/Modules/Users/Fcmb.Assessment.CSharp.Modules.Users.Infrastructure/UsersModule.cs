using Fcmb.Assessment.CSharp.Common.Application.Authorization;
using Fcmb.Assessment.CSharp.Common.Infrastructure.AuditLog;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;
using Fcmb.Assessment.CSharp.Common.Presentation.Extensions;
using Fcmb.Assessment.CSharp.Modules.Users.Application.Data;
using Fcmb.Assessment.CSharp.Modules.Users.Application.Integrations.KeyCloak;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Authorization;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Configurations;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Database;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Integrations;
using Fcmb.Assessment.CSharp.Modules.Users.Presentation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure;

public static class UsersModule
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddUsersModule(IConfiguration configuration)
        {
            services.AddUsersOptions();

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
                    .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>(),
                        sp.GetRequiredService<InsertAuditLogsInterceptor>()));

            services.AddScoped<IUnitOfWork, UnitOfWork>();
        }

        private void AddHttpClients(IConfiguration configuration)
        {
            string baseUrl = configuration[KeyCloakSettings.Path + ":BaseUrl"]!;

            services.AddTransient<KeycloakAuthHandler>();

            services.AddRefitGeneratedClient<IKeyCloakClient>()
                .ConfigureHttpClient(client => client.BaseAddress = new Uri(baseUrl))
                .AddHttpMessageHandler<KeycloakAuthHandler>();
        }
    }
}
