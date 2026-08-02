using FinancialTransaction.Application.Accounts.Dtos;

namespace FinancialTransaction.Application.Accounts;

public interface IAccountService
{
    Task<IReadOnlyList<AccountResponse>> GetAllAsync(CancellationToken cancellationToken = default);
}
