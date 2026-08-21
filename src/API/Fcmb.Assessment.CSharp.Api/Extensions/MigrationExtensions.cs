using Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Fcmb.Assessment.CSharp.Api.Extensions;

internal static class MigrationExtensions
{
    extension(IApplicationBuilder app)
    {
        internal void ApplyMigrations()
        {
            using IServiceScope scope = app.ApplicationServices.CreateScope();
            ApplyMigration<TransactionsDbContext>(scope);
        }
    }

    private static void ApplyMigration<TDbContext>(IServiceScope scope)
        where TDbContext : DbContext
    {
        using TDbContext context = scope.ServiceProvider.GetRequiredService<TDbContext>();
        context.Database.Migrate();
    }
}
