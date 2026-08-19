using System.Diagnostics;
using FinancialTransaction.Api.Endpoints;
using FinancialTransaction.Api.ExceptionHandling;
using FinancialTransaction.Application;
using FinancialTransaction.Application.Common.Telemetry;
using FinancialTransaction.Infrastructure;
using FinancialTransaction.Infrastructure.Persistence;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

const string ServiceName = "FinancialTransaction.Api";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// OpenTelemetry — instrumentação SOMENTE desta API (HTTP recebido, HttpClient de saída e EF Core/PostgreSQL).
// Sem Collector/Jaeger nesta fase: os traces são exportados para o Console para validação local.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: ServiceName))
    .WithTracing(tracing => tracing
        .AddSource(ServiceName)
        .AddSource(ApplicationDiagnostics.SourceName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddConsoleExporter());

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
