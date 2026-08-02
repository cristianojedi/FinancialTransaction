using FinancialTransaction.Domain.Common;

namespace FinancialTransaction.Domain.Events;

public sealed class TransactionFailed(Guid transactionId, string reason) : IDomainEvent
{
    public Guid TransactionId { get; } = transactionId;

    public string Reason { get; } = reason;

    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
