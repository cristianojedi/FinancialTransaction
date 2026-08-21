using System.Diagnostics.Metrics;

namespace FinancialTransaction.Application.Common.Telemetry;

/// <summary>
/// Métricas manuais emitidas pela camada Application. Precisa ser registrado explicitamente no
/// provider de métricas (AddMeter) para que os instrumentos sejam exportados.
/// </summary>
public static class ApplicationMetrics
{
    public const string MeterName = "FinancialTransaction.Application";

    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> TransactionsCreated = Meter.CreateCounter<long>(
        "financial_transaction.transactions.created",
        unit: "{transaction}",
        description: "Número de transações financeiras criadas.");

    public static readonly Counter<long> TransactionsProcessed = Meter.CreateCounter<long>(
        "financial_transaction.transactions.processed",
        unit: "{transaction}",
        description: "Número de transações financeiras processadas, marcadas com a tag 'status' (Processed/Failed).");

    public static readonly Histogram<double> TransactionProcessingDuration = Meter.CreateHistogram<double>(
        "financial_transaction.transaction.processing.duration",
        unit: "ms",
        description: "Duração do processamento de uma transação (TransactionProcessingService.ProcessAsync), em milissegundos.");
}
