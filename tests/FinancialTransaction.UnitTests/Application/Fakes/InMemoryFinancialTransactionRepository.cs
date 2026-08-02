using FinancialTransaction.Application.Abstractions.Persistence;
using TransactionEntity = FinancialTransaction.Domain.Entities.FinancialTransaction;

namespace FinancialTransaction.UnitTests.Application.Fakes;

public class InMemoryFinancialTransactionRepository : IFinancialTransactionRepository
{
    private readonly Dictionary<Guid, TransactionEntity> _transactions = [];

    public Task AddAsync(TransactionEntity transaction, CancellationToken cancellationToken = default)
    {
        _transactions[transaction.Id] = transaction;
        return Task.CompletedTask;
    }

    public Task<TransactionEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_transactions.GetValueOrDefault(id));

    public Task<IReadOnlyList<TransactionEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TransactionEntity>>(_transactions.Values.ToList());
}
