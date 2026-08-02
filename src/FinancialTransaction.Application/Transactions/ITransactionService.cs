using FinancialTransaction.Application.Transactions.Dtos;

namespace FinancialTransaction.Application.Transactions;

public interface ITransactionService
{
    Task<TransactionResponse> CreateAsync(CreateTransactionRequest request, CancellationToken cancellationToken = default);

    Task<TransactionResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
