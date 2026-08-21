using FinancialTransaction.Application;
using FinancialTransaction.Application.Common.Telemetry;
using FinancialTransaction.Infrastructure;
using FinancialTransaction.Worker;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

const string ServiceName = "FinancialTransaction.Worker";

var builder = Host.CreateApplicationBuilder(args);

// Endpoint base do Collector (sem sufixo de sinal) — cada exporter OTLP abaixo anexa /v1/traces ou /v1/metrics,
// pois o sufixo não é anexado automaticamente quando o Endpoint é definido em código (só quando configurado via
// a variável de ambiente OTEL_EXPORTER_OTLP_ENDPOINT).
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4318";

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHostedService<Worker>();

// OpenTelemetry — instrumentação deste Worker (consumo Kafka, processamento da transação e EF Core/PostgreSQL).
// O span de consumo (WorkerDiagnostics) extrai o traceparent dos headers Kafka gravados pelo Producer da API,
// continuando o mesmo trace distribuído em vez de iniciar um trace novo. Desde a Fase 13, traces e (a partir da
// Fase 14) métricas são exportados via OTLP/HTTP para o OpenTelemetry Collector
// (infrastructure/docker/observability/otel-collector-config.yaml), que reexporta traces para o Jaeger e
// métricas para o Prometheus, ambos consultados pelo Grafana.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: ServiceName))
    .WithTracing(tracing => tracing
        .AddSource(WorkerDiagnostics.SourceName)
        .AddSource(ApplicationDiagnostics.SourceName)
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = new Uri($"{otlpEndpoint}/v1/traces");
            otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
        }))
    .WithMetrics(metrics => metrics
        .AddMeter(WorkerMetrics.MeterName)
        .AddMeter(ApplicationMetrics.MeterName)
        .AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = new Uri($"{otlpEndpoint}/v1/metrics");
            otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
        }));

var host = builder.Build();
host.Run();
