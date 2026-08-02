using FinancialTransaction.Domain.Common;

namespace FinancialTransaction.Domain.Events;

public sealed class TransactionFailed : IDomainEvent
{
    public TransactionFailed(Guid transactionId, string reason)
    {
        TransactionId = transactionId;
        Reason = reason;
        OccurredOnUtc = DateTime.UtcNow;
    }

    public Guid TransactionId { get; }

    public string Reason { get; }

    public DateTime OccurredOnUtc { get; }
}
