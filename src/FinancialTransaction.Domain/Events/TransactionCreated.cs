using FinancialTransaction.Domain.Common;

namespace FinancialTransaction.Domain.Events;

public sealed class TransactionCreated(Guid transactionId, Guid sourceAccountId, Guid destinationAccountId, decimal amount) : IDomainEvent
{
    public Guid TransactionId { get; } = transactionId;

    public Guid SourceAccountId { get; } = sourceAccountId;

    public Guid DestinationAccountId { get; } = destinationAccountId;

    public decimal Amount { get; } = amount;

    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
