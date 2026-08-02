using FinancialTransaction.Domain.Common;
using FinancialTransaction.Domain.Enums;
using FinancialTransaction.Domain.Events;
using FinancialTransaction.Domain.Exceptions;

namespace FinancialTransaction.Domain.Entities;

public class FinancialTransaction : AggregateRoot
{
    public Guid SourceAccountId { get; private set; }

    public Guid DestinationAccountId { get; private set; }

    public decimal Amount { get; private set; }

    public TransactionStatus Status { get; private set; }

    public string? FailureReason { get; private set; }

    private FinancialTransaction(Guid id, Guid sourceAccountId, Guid destinationAccountId, decimal amount)
        : base(id)
    {
        SourceAccountId = sourceAccountId;
        DestinationAccountId = destinationAccountId;
        Amount = amount;
        Status = TransactionStatus.Pending;
    }

    public static FinancialTransaction Create(Guid sourceAccountId, Guid destinationAccountId, decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainException("O valor da transação deve ser maior que zero.");
        }

        if (sourceAccountId == destinationAccountId)
        {
            throw new DomainException("A conta de origem deve ser diferente da conta de destino.");
        }

        var transaction = new FinancialTransaction(Guid.NewGuid(), sourceAccountId, destinationAccountId, amount);

        transaction.RaiseDomainEvent(new TransactionCreated(transaction.Id, sourceAccountId, destinationAccountId, amount));

        return transaction;
    }

    public void StartProcessing()
    {
        if (Status != TransactionStatus.Pending)
        {
            throw new DomainException($"Não é possível iniciar o processamento de uma transação no estado {Status}.");
        }

        Status = TransactionStatus.Processing;
    }

    public void CompleteProcessing()
    {
        if (Status != TransactionStatus.Processing)
        {
            throw new DomainException($"Não é possível concluir o processamento de uma transação no estado {Status}.");
        }

        Status = TransactionStatus.Processed;

        RaiseDomainEvent(new TransactionProcessed(Id));
    }

    public void FailProcessing(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("O motivo da falha é obrigatório.", nameof(reason));
        }

        if (Status != TransactionStatus.Processing)
        {
            throw new DomainException($"Não é possível falhar uma transação no estado {Status}.");
        }

        Status = TransactionStatus.Failed;
        FailureReason = reason;

        RaiseDomainEvent(new TransactionFailed(Id, reason));
    }
}
