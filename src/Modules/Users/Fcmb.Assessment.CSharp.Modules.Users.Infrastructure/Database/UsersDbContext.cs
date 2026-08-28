using Fcmb.Assessment.CSharp.Common.Infrastructure;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Emails;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.PhoneNumbers;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Database;

public sealed class UsersDbContext(
    DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Email> Emails { get; set; }
    public DbSet<PhoneNumber> PhoneNumbers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Users);

        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(AssemblyReference.Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);
    }
}
