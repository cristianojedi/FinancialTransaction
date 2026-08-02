namespace FinancialTransaction.Application.Abstractions.Persistence;

public interface IFinancialTransactionRepository
{
    Task AddAsync(Domain.Entities.FinancialTransaction transaction, CancellationToken cancellationToken = default);

    Task<Domain.Entities.FinancialTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Domain.Entities.FinancialTransaction>> GetAllAsync(CancellationToken cancellationToken = default);
}
