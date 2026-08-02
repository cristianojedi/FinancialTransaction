using FinancialTransaction.Application.Accounts;
using FinancialTransaction.Application.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialTransaction.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ITransactionProcessingService, TransactionProcessingService>();

        return services;
    }
}
