using System.Diagnostics;
using FinancialTransaction.Api.Endpoints;
using FinancialTransaction.Api.ExceptionHandling;
using FinancialTransaction.Application;
using FinancialTransaction.Application.Common.Telemetry;
using FinancialTransaction.Infrastructure;
using FinancialTransaction.Infrastructure.Messaging;
using FinancialTransaction.Infrastructure.Persistence;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

const string ServiceName = "FinancialTransaction.Api";

var builder = WebApplication.CreateBuilder(args);

// Endpoint base do Collector (sem sufixo de sinal) — cada exporter OTLP abaixo anexa /v1/traces ou /v1/metrics,
// pois o sufixo não é anexado automaticamente quando o Endpoint é definido em código (só quando configurado via
// a variável de ambiente OTEL_EXPORTER_OTLP_ENDPOINT).
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4318";

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// OpenTelemetry — instrumentação desta API (HTTP recebido, HttpClient de saída, EF Core/PostgreSQL e publicação Kafka).
// O span de publicação Kafka (InfrastructureDiagnostics) injeta o traceparent nos headers da mensagem,
// permitindo que o Worker continue o mesmo trace ao consumir. Desde a Fase 13, traces e (a partir da Fase 14)
// métricas são exportados via OTLP/HTTP para o OpenTelemetry Collector
// (infrastructure/docker/observability/otel-collector-config.yaml), que reexporta traces para o Jaeger e
// métricas para o Prometheus, ambos consultados pelo Grafana.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: ServiceName))
    .WithTracing(tracing => tracing
        .AddSource(ServiceName)
        .AddSource(ApplicationDiagnostics.SourceName)
        .AddSource(InfrastructureDiagnostics.SourceName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = new Uri($"{otlpEndpoint}/v1/traces");
            otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
        }))
    .WithMetrics(metrics => metrics
        .AddMeter(ApplicationMetrics.MeterName)
        .AddMeter(InfrastructureMetrics.MeterName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = new Uri($"{otlpEndpoint}/v1/metrics");
            otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
        }));

var app = builder.Build();

app.UseExceptionHandler();

// Expõe o TraceId da requisição atual no header de resposta, útil para validar manualmente
// (Swagger/Postman) que um trace foi gerado para aquela chamada HTTP.
app.Use(async (context, next) =>
{
    var traceId = Activity.Current?.TraceId.ToString();
    if (traceId is not null)
    {
        context.Response.Headers["X-Trace-Id"] = traceId;
    }

    await next();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "FinancialTransaction API v1");
        options.RoutePrefix = "swagger";
    });

    await AccountsSeeder.SeedAsync(app.Services, app.Logger);
}

app.UseHttpsRedirection();

app.MapAccountEndpoints();
app.MapTransactionEndpoints();

app.Run();

public partial class Program
{
}
