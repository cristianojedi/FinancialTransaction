namespace FinancialTransaction.Application.Transactions.Dtos;

public record TransactionResponse(
    Guid Id,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Status,
    string? FailureReason,
    DateTime CreatedAtUtc)
{
    public static TransactionResponse FromDomain(Domain.Entities.FinancialTransaction transaction) =>
        new(
            transaction.Id,
            transaction.SourceAccountId,
            transaction.DestinationAccountId,
            transaction.Amount,
            transaction.Status.ToString(),
            transaction.FailureReason,
            transaction.CreatedAtUtc);
}
