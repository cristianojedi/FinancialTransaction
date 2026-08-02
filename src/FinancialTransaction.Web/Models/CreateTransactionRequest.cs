namespace FinancialTransaction.Web.Models;

public record CreateTransactionRequest(Guid SourceAccountId, Guid DestinationAccountId, decimal Amount);
