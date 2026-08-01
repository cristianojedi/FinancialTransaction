# FinancialTransaction — Fase 13: OpenTelemetry Collector

> Prompt de execução para Claude Code.
>
> **Como usar:** execute este prompt dentro da raiz do repositório `FinancialTransaction`.
> Leia o `PROJECT_GUIDE.md` antes de começar. Implemente somente esta fase.
> Ao concluir, execute os testes/validações descritos, atualize a documentação quando solicitado e pare.
> Não avance automaticamente para a próxima fase.

---

# Fase 13 — OpenTelemetry Collector

## Objetivo

Depois que API, Worker e propagação distribuída estiverem funcionando, introduzir o OpenTelemetry Collector.

Arquitetura:

```text
FinancialTransaction.Api
          │
          │ OTLP
          ▼
┌───────────────────────┐
│ OpenTelemetry         │
│ Collector              │
└───────────┬───────────┘
            │
            │ OTLP
            ▼
       Observability


FinancialTransaction.Worker
          │
          │ OTLP
          ▼
┌───────────────────────┐
│ OpenTelemetry         │
│ Collector              │
└───────────┬───────────┘
            │
            ▼
       Observability
```

### Conceitos

Estudar:

- OTLP;
- Collector;
- Receiver;
- Processor;
- Exporter;
- Pipeline;
- Batch Processor;
- Resource Attributes;
- Environment Attributes.

### Definition of Done

- [ ] Collector em Docker;
- [ ] API envia telemetria;
- [ ] Worker envia telemetria;
- [ ] Collector recebe;
- [ ] pipeline configurado;
- [ ] configuração documentada.

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Até agora temos:

1. OpenTelemetry na API.
2. OpenTelemetry no Worker.
3. OpenTelemetry no Kafka Producer.
4. OpenTelemetry no Kafka Consumer.
5. Propagação de Trace Context através do Kafka.
6. Distributed Tracing funcionando.

Agora introduza o OpenTelemetry Collector.

Explique detalhadamente:

1. O que é OpenTelemetry Collector.
2. Por que usar Collector.
3. O que é OTLP.
4. Receiver.
5. Processor.
6. Exporter.
7. Pipeline.
8. Batch Processor.
9. Resource Attributes.

Crie uma configuração Docker Compose para o Collector.

API e Worker devem enviar telemetria para o Collector.

O Collector deverá receber:

- Traces.
- Metrics, quando aplicável.
- Logs, quando aplicável.

Mantenha a configuração simples e didática.

Explique o fluxo:

API
 -> OTLP
 -> OTel Collector

Worker
 -> OTLP
 -> OTel Collector

Não implemente ainda dashboards finais.

Ao final explique como validar que o Collector está recebendo dados.
```

---