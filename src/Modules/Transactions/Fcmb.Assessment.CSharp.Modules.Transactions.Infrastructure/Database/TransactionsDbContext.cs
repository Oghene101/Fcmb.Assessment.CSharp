using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Accounts;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Customers;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Rewards;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure.Database;

public sealed class TransactionsDbContext(
    DbContextOptions<TransactionsDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Reward> Rewards { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Transactions);
        
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Common.Infrastructure.AssemblyReference.Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransactionsDbContext).Assembly);
    }
}
