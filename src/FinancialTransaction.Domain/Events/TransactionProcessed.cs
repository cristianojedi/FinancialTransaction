using FinancialTransaction.Domain.Common;

namespace FinancialTransaction.Domain.Events;

public sealed class TransactionProcessed : IDomainEvent
{
    public TransactionProcessed(Guid transactionId)
    {
        TransactionId = transactionId;
        OccurredOnUtc = DateTime.UtcNow;
    }

    public Guid TransactionId { get; }

    public DateTime OccurredOnUtc { get; }
}
