using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using FinancialTransaction.Application.Abstractions.Messaging;
using FinancialTransaction.Domain.Common;
using FinancialTransaction.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

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

        // Span de publicação (ActivityKind.Producer): filho do trace atual (ex.: TransactionService.CreateAsync)
        // e ponto de partida do trace distribuído que o Worker vai continuar ao consumir a mensagem.
        using var activity = InfrastructureDiagnostics.ActivitySource.StartActivity(
            $"{topic} publish",
            ActivityKind.Producer);

        activity?.SetTag("messaging.system", "kafka");
        activity?.SetTag("messaging.destination", topic);
        activity?.SetTag("messaging.kafka.message_key", key);
        activity?.SetTag("event.type", eventType.Name);

        if (domainEvent is TransactionCreated transactionCreated)
        {
            activity?.SetTag("transaction.id", transactionCreated.TransactionId);
        }

        var message = new Message<string, string>
        {
            Key = key,
            Value = JsonSerializer.Serialize(domainEvent, eventType),
            Headers = new Headers
            {
                { "event-type", Encoding.UTF8.GetBytes(eventType.Name) },
            },
        };

        // Inject: grava o traceparent (e, se houver, o baggage) do span atual nos headers Kafka.
        // Sem isso, a mensagem chega ao Worker sem nenhuma informação de trace e o Consumer inicia um trace novo e desconectado.
        var propagationContext = new PropagationContext(Activity.Current?.Context ?? default, Baggage.Current);
        Propagators.DefaultTextMapPropagator.Inject(propagationContext, message.Headers, InjectHeader);

        try
        {
            var result = await _producer.ProduceAsync(topic, message, cancellationToken);

            activity?.SetTag("messaging.kafka.partition", result.Partition.Value);
            activity?.SetTag("messaging.kafka.offset", result.Offset.Value);

            _logger.LogInformation(
                "Evento {EventType} publicado no topic {Topic} (partition {Partition}, offset {Offset}).",
                eventType.Name,
                topic,
                result.Partition.Value,
                result.Offset.Value);

            InfrastructureMetrics.KafkaMessagesPublished.Add(1,
                new KeyValuePair<string, object?>("topic", topic));
        }
        catch (ProduceException<string, string> ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            _logger.LogError(
                ex,
                "Falha ao publicar evento {EventType} no topic {Topic}: {Reason}.",
                eventType.Name,
                topic,
                ex.Error.Reason);

            InfrastructureMetrics.KafkaPublishErrors.Add(1,
                new KeyValuePair<string, object?>("topic", topic));
            throw;
        }
    }

    private static void InjectHeader(Headers headers, string key, string value) =>
        headers.Add(key, Encoding.UTF8.GetBytes(value));

    private (string Topic, string Key) Resolve(IDomainEvent domainEvent) => domainEvent switch
    {
        TransactionCreated e => (_options.TransactionsTopic, e.TransactionId.ToString()),
        _ => throw new InvalidOperationException(
            $"Não há topic Kafka configurado para o evento '{domainEvent.GetType().Name}'."),
    };

    public void Dispose() => _producer.Dispose();
}
