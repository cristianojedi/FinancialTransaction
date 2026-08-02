namespace FinancialTransaction.Application.Transactions.Dtos;

public record CreateTransactionRequest(Guid SourceAccountId, Guid DestinationAccountId, decimal Amount);
