using Microsoft.EntityFrameworkCore;
using CommonInfrastructure = Fcmb.Assessment.CSharp.Common.Infrastructure.AssemblyReference;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.UnitTests.Abstractions;

internal sealed class MessagingTestDbContext(
    DbContextOptions<MessagingTestDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(CommonInfrastructure.Assembly);
    }
}
