namespace FinancialTransaction.Application.Accounts.Dtos;

public record AccountResponse(Guid Id, string Number)
{
    public static AccountResponse FromDomain(Domain.Entities.Account account) =>
        new(account.Id, account.Number);
}
