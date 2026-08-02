using FinancialTransaction.Domain.Common;

namespace FinancialTransaction.Domain.Events;

public sealed class TransactionProcessed(Guid transactionId) : IDomainEvent
{
    public Guid TransactionId { get; } = transactionId;

    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
