namespace FinancialTransaction.Web.Services;

public sealed class ApiException(string message) : Exception(message);
