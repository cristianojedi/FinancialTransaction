using FinancialTransaction.Application;
using FinancialTransaction.Application.Common.Telemetry;
using FinancialTransaction.Infrastructure;
using FinancialTransaction.Worker;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

const string ServiceName = "FinancialTransaction.Worker";

var builder = Host.CreateApplicationBuilder(args);

var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4318/v1/traces";

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHostedService<Worker>();

// OpenTelemetry — instrumentação deste Worker (consumo Kafka, processamento da transação e EF Core/PostgreSQL).
// O span de consumo (WorkerDiagnostics) extrai o traceparent dos headers Kafka gravados pelo Producer da API,
// continuando o mesmo trace distribuído em vez de iniciar um trace novo. A partir da Fase 13, os traces são
// exportados via OTLP/HTTP para o OpenTelemetry Collector (infrastructure/docker/observability/otel-collector-config.yaml),
// que os imprime no próprio log (exporter "debug") — ainda sem Jaeger/Tempo/Grafana, que chegam na Fase 14.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: ServiceName))
    .WithTracing(tracing => tracing
        .AddSource(WorkerDiagnostics.SourceName)
        .AddSource(ApplicationDiagnostics.SourceName)
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = new Uri(otlpEndpoint);
            otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
        }));

var host = builder.Build();
host.Run();
