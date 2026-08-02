namespace FinancialTransaction.Web.Models;

public record TransactionResponse(
    Guid Id,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Status,
    string? FailureReason);
