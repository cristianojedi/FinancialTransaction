using FinancialTransaction.Web.Models;

namespace FinancialTransaction.Web.Services;

public interface IFinancialApiClient
{
    Task<IReadOnlyList<AccountResponse>> GetAccountsAsync(CancellationToken ct = default);

    Task<TransactionResponse> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken ct = default);
}
