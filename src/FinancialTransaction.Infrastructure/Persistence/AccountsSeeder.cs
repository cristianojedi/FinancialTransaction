using FinancialTransaction.Application.Abstractions.Persistence;
using FinancialTransaction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FinancialTransaction.Infrastructure.Persistence;

public static class AccountsSeeder
{
    private static readonly string[] AccountNumbers = ["ACC-001", "ACC-002"];

    public static async Task SeedAsync(IServiceProvider services, ILogger logger, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<FinancialTransactionDbContext>();

        if (await dbContext.Accounts.AnyAsync(cancellationToken))
        {
            return;
        }

        var accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var seededAccounts = new List<Account>();

        foreach (var number in AccountNumbers)
        {
            var account = Account.Create(number);
            await accountRepository.AddAsync(account, cancellationToken);
            seededAccounts.Add(account);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var account in seededAccounts)
        {
            logger.LogInformation("Conta de teste criada: {Number} -> {Id}", account.Number, account.Id);
        }
    }
}
