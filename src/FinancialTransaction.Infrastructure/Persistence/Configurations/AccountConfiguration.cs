using FinancialTransaction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialTransaction.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(account => account.Id);

        builder.Property(account => account.Id)
            .ValueGeneratedNever();

        builder.Property(account => account.Number)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(account => account.Number)
            .IsUnique();
    }
}
