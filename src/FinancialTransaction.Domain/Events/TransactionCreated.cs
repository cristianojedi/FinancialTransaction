using FinancialTransaction.Domain.Common;

namespace FinancialTransaction.Domain.Events;

public sealed class TransactionCreated : IDomainEvent
{
    public TransactionCreated(Guid transactionId, Guid sourceAccountId, Guid destinationAccountId, decimal amount)
    {
        TransactionId = transactionId;
        SourceAccountId = sourceAccountId;
        DestinationAccountId = destinationAccountId;
        Amount = amount;
        OccurredOnUtc = DateTime.UtcNow;
    }

    public Guid TransactionId { get; }

    public Guid SourceAccountId { get; }

    public Guid DestinationAccountId { get; }

    public decimal Amount { get; }

    public DateTime OccurredOnUtc { get; }
}
