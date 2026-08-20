# Observabilidade — Fase 13: OpenTelemetry Collector

> Escopo desta fase: introduzir o **OpenTelemetry Collector** entre a Api/Worker e o "backend" de observabilidade.
> Nesta fase o Collector ainda não exporta para Jaeger/Tempo/Prometheus/Grafana — apenas recebe, processa e
> imprime a telemetria no próprio log (exporter `debug`), para provar que o pipeline está funcionando. Backends
> reais entram na [Fase 14](../docs/FASE_14_JAEGER_TEMPO_PROMETHEUS_GRAFANA.md).

## 1. Conceitos

### 1.1 OpenTelemetry Collector

O Collector é um processo (binário/container) independente das aplicações, cuja única responsabilidade é
receber, processar e reexportar telemetria (traces, métricas, logs). Ele fica entre "quem gera" telemetria (Api,
Worker) e "quem consome" (Jaeger, Prometheus, Grafana, qualquer backend OTLP-compatível).

### 1.2 Por que usar Collector

Sem Collector, cada aplicação precisaria exportar diretamente para cada backend de observabilidade (Jaeger,
Prometheus, etc.), acoplando a aplicação à infraestrutura de observabilidade e obrigando a reconfigurar/recompilar
o serviço sempre que o backend mudar. Com o Collector:

- a aplicação só conhece **um** destino (o Collector, via OTLP);
- a troca de backend (Jaeger → Tempo, por exemplo) é uma mudança de configuração do Collector, não do código;
- processamento comum (batching, enriquecimento com atributos, filtragem, amostragem) acontece em um único lugar,
  fora do processo da aplicação;
- é possível fan-out: um único trace pode ser enviado para múltiplos backends ao mesmo tempo.

### 1.3 OTLP

OTLP (OpenTelemetry Protocol) é o protocolo de transporte padrão do OpenTelemetry, usado tanto entre
aplicação → Collector quanto entre Collector → backend. Suporta gRPC (porta padrão `4317`) e HTTP/protobuf (porta
padrão `4318`). Nesta fase, Api e Worker usam **HTTP/protobuf**, por ser mais simples de configurar localmente do
que gRPC em texto claro (cleartext HTTP/2), que exige configuração adicional no `HttpClient` do .NET.

### 1.4 Receiver

Componente do Collector que **recebe** telemetria de fora. Nesta fase, o `otlp` receiver, que abre um endpoint
gRPC (`4317`) e um HTTP (`4318`) e aceita payloads no formato OTLP — exatamente o que o SDK OpenTelemetry da Api e
do Worker envia.

### 1.5 Processor

Componente que **transforma** a telemetria depois de recebida e antes de exportada. Usados nesta fase:

- **`batch`**: agrupa múltiplos spans em lotes antes de exportar, em vez de uma chamada de rede por span.
- **`resource`**: adiciona/normaliza Resource Attributes em tudo que passa pelo pipeline.

### 1.6 Exporter

Componente que **envia** a telemetria processada para um destino. Nesta fase, o exporter `debug`, que apenas
imprime a telemetria recebida no log do próprio Collector — o equivalente ao `ConsoleExporter` que a Api e o
Worker usavam nas Fases 10-12, só que agora do lado do Collector. Nenhum backend real (Jaeger, Tempo, Prometheus)
é usado ainda.

### 1.7 Pipeline

Um pipeline conecta um ou mais **receivers** → **processors** → **exporters** para um tipo de sinal (traces,
metrics ou logs). Nesta fase existe apenas o pipeline de `traces`:

```yaml
service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [resource, batch]
      exporters: [debug]
```

### 1.8 Batch Processor

Ver 1.5. É citado separadamente porque é, na prática, o processor mais importante de qualquer pipeline de
produção: evita que cada span vire uma chamada de rede isolada, reduzindo drasticamente o overhead de exportação.

### 1.9 Resource Attributes

Atributos que identificam **a origem** da telemetria — de qual processo, ambiente, versão, host ela veio. A Api
e o Worker já definem o atributo `service.name` (via `ConfigureResource(...).AddService(serviceName: ...)`, desde
a Fase 10/11). O Collector, nesta fase, complementa com `deployment.environment: local` via processor `resource`,
mostrando que Resource Attributes podem ser adicionados tanto na origem (aplicação) quanto no meio do caminho
(Collector).

### 1.10 Environment Attributes

Caso particular de Resource Attributes que descreve o ambiente de execução (`deployment.environment`,
`host.name`, etc.), permitindo diferenciar, no mesmo backend, telemetria vinda de `local`, `staging` ou
`production` sem precisar de instâncias de observabilidade separadas.

## 2. O que foi implementado

```text
FinancialTransaction.Api
          │
          │ OTLP/HTTP (:4318)
          ▼
┌───────────────────────┐
│ OpenTelemetry          │
│ Collector              │
│  receiver: otlp        │
│  processors: resource, │
│              batch     │
│  exporter: debug       │
└────────────────────────┘

FinancialTransaction.Worker
          │
          │ OTLP/HTTP (:4318)
          ▼
┌───────────────────────┐
│ OpenTelemetry          │
│ Collector (mesmo)      │
└────────────────────────┘
```

### 2.1 Collector em Docker

Serviço adicionado a [docker-compose.yml](../docker-compose.yml):

```yaml
otel-collector:
  image: otel/opentelemetry-collector:0.116.1
  container_name: financialtransaction_otel_collector
  command: ["--config=/etc/otelcol/config.yaml"]
  volumes:
    - ./infrastructure/docker/observability/otel-collector-config.yaml:/etc/otelcol/config.yaml:ro
  ports:
    - "4317:4317" # OTLP gRPC
    - "4318:4318" # OTLP HTTP/protobuf — usado pela Api e pelo Worker nesta fase
  networks:
    - financialtransaction-network
```

Usa a imagem **core** (não `-contrib`) porque o único exporter necessário nesta fase (`debug`) já está incluído
nela — não há dependência de exporters específicos de backend ainda.

Configuração completa:
[infrastructure/docker/observability/otel-collector-config.yaml](../infrastructure/docker/observability/otel-collector-config.yaml).

### 2.2 Api e Worker enviando telemetria via OTLP

Antes (Fases 10-12): `AddConsoleExporter()` — os traces eram impressos no console de cada processo.

Depois (Fase 13): `AddOtlpExporter(...)` — os traces são enviados via HTTP/protobuf para o Collector.

[Program.cs da Api](../src/FinancialTransaction.Api/Program.cs):

```csharp
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4318/v1/traces";

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
            otlp.Endpoint = new Uri(otlpEndpoint);
            otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
        }));
```

[Program.cs do Worker](../src/FinancialTransaction.Worker/Program.cs) segue o mesmo padrão, trocando apenas as
fontes registradas (`WorkerDiagnostics`, `ApplicationDiagnostics`).

O endpoint é configurável via `appsettings.json` (seção `OpenTelemetry:OtlpEndpoint`), com o valor
`http://localhost:4318/v1/traces` como padrão para o Nível 1/2 de desenvolvimento (aplicações locais, infra em
Docker com portas publicadas no host). O caminho `/v1/traces` é exigido explicitamente porque, ao configurar
`Endpoint` diretamente em código (em vez da variável de ambiente `OTEL_EXPORTER_OTLP_ENDPOINT`), o SDK não anexa
o sufixo do sinal automaticamente.

Pacote trocado em ambos os `.csproj`: `OpenTelemetry.Exporter.Console` → `OpenTelemetry.Exporter.OpenTelemetryProtocol`.

### 2.3 Traces, Metrics e Logs

O receiver `otlp` do Collector aceita os três tipos de sinal (traces, metrics, logs) simultaneamente — não é
necessário configurar nada extra para "ligar" cada um. Nesta fase, porém, apenas o pipeline de **traces** foi
declarado em `service.pipelines`, porque é o único sinal que a Api e o Worker emitem via OpenTelemetry até agora
(não há `MeterProvider`/`LoggerProvider` configurado nas Fases 10-12). Métricas e logs ficam para fases futuras —
quando existirem, bastará configurar `.WithMetrics(...)` / `.WithLogging(...)` na aplicação e adicionar os
pipelines `metrics`/`logs` correspondentes no Collector, reaproveitando o mesmo receiver `otlp`.

## 3. Como validar que o Collector está recebendo dados

### 3.1 Subir a infraestrutura (inclui o Collector)

```bash
docker compose up -d
docker compose ps
```

### 3.2 Rodar Api e Worker localmente

```bash
dotnet run --project src/FinancialTransaction.Api
dotnet run --project src/FinancialTransaction.Worker
```

### 3.3 Criar uma transação

```bash
curl -i -X POST http://localhost:5209/api/transactions \
  -H "Content-Type: application/json" \
  -d '{"sourceAccountId":"<id-conta-origem>","destinationAccountId":"<id-conta-destino>","amount":150.00}'
```

A resposta traz `X-Trace-Id: <traceId>`.

### 3.4 Ler o TraceId no log do Collector

```bash
docker compose logs otel-collector | grep -A5 "<traceId>"
```

Validado nesta implementação com o TraceId `c80f6ef42e37131c16eeb7ef952a39a6`: os spans da Api
(`Microsoft.AspNetCore`, `FinancialTransaction.Application`, `OpenTelemetry.Instrumentation.EntityFrameworkCore`,
`FinancialTransaction.Infrastructure` — publicação Kafka) e do Worker (`FinancialTransaction.Worker`,
`FinancialTransaction.Application`, EF Core) apareceram **todos com o mesmo TraceId** no exporter `debug` do
Collector — confirmando que:

1. a Api conseguiu falar OTLP/HTTP com o Collector (`http://localhost:4318/v1/traces`);
2. o Worker conseguiu falar OTLP/HTTP com o mesmo Collector;
3. o Collector recebeu (`receiver: otlp`), processou (`resource` + `batch`) e exportou (`debug`) os spans de
   ambos os serviços;
4. o trace distribuído (Fase 12) continua correlacionado corretamente mesmo passando pelo Collector no meio do
   caminho — Api e Worker exportam para o mesmo Collector, mas cada um mantém seu próprio `TracerProvider` e
   contexto de trace, propagado via headers Kafka como já validado na Fase 12.

Também é possível observar, no log de inicialização do Collector, a confirmação de que os receivers estão de pé:

```text
info otlpreceiver@v0.116.0/otlp.go:112 Starting GRPC server {"endpoint": "0.0.0.0:4317"}
info otlpreceiver@v0.116.0/otlp.go:169 Starting HTTP server {"endpoint": "0.0.0.0:4318"}
```

## 4. Fora do escopo desta fase (propositalmente)

- Exportação para Jaeger/Tempo, Prometheus ou Grafana (Fase 14).
- Dashboards.
- Pipelines de `metrics` e `logs` no Collector (a aplicação ainda não emite esses sinais via OpenTelemetry).
- Amostragem (`sampling`), filtragem ou qualquer processor além de `resource`/`batch`.
