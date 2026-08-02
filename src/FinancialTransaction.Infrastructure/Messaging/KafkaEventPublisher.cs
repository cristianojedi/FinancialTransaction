using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using FinancialTransaction.Application.Abstractions.Messaging;
using FinancialTransaction.Domain.Common;
using FinancialTransaction.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinancialTransaction.Infrastructure.Messaging;

public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(
        IProducer<string, string> producer,
        IOptions<KafkaOptions> options,
        ILogger<KafkaEventPublisher> logger)
    {
        _producer = producer;
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var (topic, key) = Resolve(domainEvent);
        var eventType = domainEvent.GetType();

        var message = new Message<string, string>
        {
            Key = key,
            Value = JsonSerializer.Serialize(domainEvent, eventType),
            Headers = new Headers
            {
                { "event-type", Encoding.UTF8.GetBytes(eventType.Name) },
            },
        };

        try
        {
            var result = await _producer.ProduceAsync(topic, message, cancellationToken);

            _logger.LogInformation(
                "Evento {EventType} publicado no topic {Topic} (partition {Partition}, offset {Offset}).",
                eventType.Name,
                topic,
                result.Partition.Value,
                result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(
                ex,
                "Falha ao publicar evento {EventType} no topic {Topic}: {Reason}.",
                eventType.Name,
                topic,
                ex.Error.Reason);
            throw;
        }
    }

    private (string Topic, string Key) Resolve(IDomainEvent domainEvent) => domainEvent switch
    {
        TransactionCreated e => (_options.TransactionsTopic, e.TransactionId.ToString()),
        _ => throw new InvalidOperationException(
            $"Não há topic Kafka configurado para o evento '{domainEvent.GetType().Name}'."),
    };

    public void Dispose() => _producer.Dispose();
}
