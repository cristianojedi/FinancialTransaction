namespace FinancialTransaction.Application.Transactions;

public interface ITransactionProcessingService
{
    Task ProcessAsync(Guid transactionId, CancellationToken cancellationToken = default);
}
