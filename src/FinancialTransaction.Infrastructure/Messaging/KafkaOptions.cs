namespace FinancialTransaction.Infrastructure.Messaging;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = string.Empty;

    public string TransactionsTopic { get; set; } = string.Empty;

    public string ConsumerGroupId { get; set; } = string.Empty;
}
