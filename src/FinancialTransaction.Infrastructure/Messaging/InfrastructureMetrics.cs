using System.Diagnostics.Metrics;

namespace FinancialTransaction.Infrastructure.Messaging;

/// <summary>
/// Métricas manuais emitidas pela publicação de mensagens Kafka. Precisa ser registrado explicitamente
/// no provider de métricas (AddMeter) para que os instrumentos sejam exportados.
/// </summary>
public static class InfrastructureMetrics
{
    public const string MeterName = "FinancialTransaction.Infrastructure";

    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> KafkaMessagesPublished = Meter.CreateCounter<long>(
        "financial_transaction.kafka.messages.published",
        unit: "{message}",
        description: "Número de mensagens publicadas com sucesso no Kafka, marcadas com a tag 'topic'.");

    public static readonly Counter<long> KafkaPublishErrors = Meter.CreateCounter<long>(
        "financial_transaction.kafka.publish.errors",
        unit: "{error}",
        description: "Número de falhas ao publicar mensagens no Kafka, marcadas com a tag 'topic'.");
}
