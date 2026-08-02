using FinancialTransaction.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinancialTransaction.Infrastructure.Persistence;

public class FinancialTransactionDbContext : DbContext
{
    public FinancialTransactionDbContext(DbContextOptions<FinancialTransactionDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Domain.Entities.FinancialTransaction> Transactions => Set<Domain.Entities.FinancialTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinancialTransactionDbContext).Assembly);
    }
}
