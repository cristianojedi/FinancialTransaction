using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialTransaction.Infrastructure.Persistence.Configurations;

public class FinancialTransactionConfiguration : IEntityTypeConfiguration<Domain.Entities.FinancialTransaction>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.FinancialTransaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Id)
            .ValueGeneratedNever();

        builder.Property(transaction => transaction.SourceAccountId)
            .IsRequired();

        builder.Property(transaction => transaction.DestinationAccountId)
            .IsRequired();

        builder.Property(transaction => transaction.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(transaction => transaction.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(transaction => transaction.FailureReason)
            .HasMaxLength(500);

        builder.Property(transaction => transaction.CreatedAtUtc)
            .IsRequired();

        builder.Ignore(transaction => transaction.DomainEvents);

        builder.HasIndex(transaction => transaction.SourceAccountId);

        builder.HasIndex(transaction => transaction.DestinationAccountId);

        builder.HasIndex(transaction => transaction.CreatedAtUtc);
    }
}
