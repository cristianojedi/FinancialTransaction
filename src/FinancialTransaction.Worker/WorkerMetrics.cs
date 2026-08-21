using System.Diagnostics.Metrics;

namespace FinancialTransaction.Worker;

/// <summary>
/// Métricas manuais emitidas pelo consumo Kafka do Worker. Precisa ser registrado explicitamente no
/// provider de métricas (AddMeter) para que os instrumentos sejam exportados.
/// </summary>
public static class WorkerMetrics
{
    public const string MeterName = "FinancialTransaction.Worker";

    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> KafkaMessagesConsumed = Meter.CreateCounter<long>(
        "financial_transaction.kafka.messages.consumed",
        unit: "{message}",
        description: "Número de mensagens consumidas do Kafka, marcadas com a tag 'topic'.");

    public static readonly Counter<long> ProcessingErrors = Meter.CreateCounter<long>(
        "financial_transaction.worker.processing.errors",
        unit: "{error}",
        description: "Número de falhas ao processar uma mensagem consumida, marcadas com a tag 'topic'.");

    public static readonly Histogram<double> MessageProcessingDuration = Meter.CreateHistogram<double>(
        "financial_transaction.worker.message.processing.duration",
        unit: "ms",
        description: "Duração total do processamento de uma mensagem consumida (consume + process + commit), em milissegundos.");
}
