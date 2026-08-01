# FinancialTransaction — Fase 10: OpenTelemetry incremental na API

> Prompt de execução para Claude Code.
>
> **Como usar:** execute este prompt dentro da raiz do repositório `FinancialTransaction`.
> Leia o `PROJECT_GUIDE.md` antes de começar. Implemente somente esta fase.
> Ao concluir, execute os testes/validações descritos, atualize a documentação quando solicitado e pare.
> Não avance automaticamente para a próxima fase.

---

# Fase 10 — OpenTelemetry incremental na API

## Objetivo

Adicionar OpenTelemetry primeiro somente à API.

A ideia é observar o fluxo síncrono:

```text
Blazor
  │
  ▼
FinancialTransaction.Api
  │
  ├── HTTP Span
  │
  ├── Application Span
  │
  └── PostgreSQL Span
```

Nesta fase ainda não vamos rastrear o Worker nem a propagação através do Kafka.

### Conceitos

Estudar:

- Observabilidade;
- Telemetria;
- Traces;
- Spans;
- TraceId;
- SpanId;
- Activity;
- Instrumentação automática;
- Instrumentação manual;
- Exportadores;
- OTLP.

### Definition of Done

- [ ] ASP.NET Core instrumentado;
- [ ] HTTP instrumentado;
- [ ] EF Core instrumentado;
- [ ] TraceId disponível;
- [ ] Spans gerados;
- [ ] Telemetria exportada;
- [ ] API observável independentemente do Kafka.

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Nesta fase implemente OpenTelemetry de forma incremental.

IMPORTANTE:
Instrumente SOMENTE a FinancialTransaction.Api.

O fluxo observado será:

Blazor
 -> FinancialTransaction.Api
 -> Application
 -> PostgreSQL

Instrumente:

- ASP.NET Core.
- HTTP.
- EF Core.

Explique detalhadamente:

1. O que é observabilidade.
2. O que é OpenTelemetry.
3. O que é Trace.
4. O que é Span.
5. O que é TraceId.
6. O que é SpanId.
7. O que é Activity no ecossistema .NET.
8. Diferença entre instrumentação automática e manual.
9. Como o OpenTelemetry coleta os dados.
10. O que é OTLP.

Configure OpenTelemetry de maneira adequada para .NET 10.

Nesta fase NÃO implemente:

- OpenTelemetry no Worker.
- Propagação de contexto através do Kafka.
- OpenTelemetry Collector.
- Grafana.
- Prometheus.
- Jaeger/Tempo.

O objetivo é conseguir observar uma requisição HTTP da API e suas operações de banco.

Ao final:

1. Execute uma transação.
2. Mostre como validar que um Trace foi criado.
3. Explique como identificar HTTP e PostgreSQL dentro do trace.
4. Documente os pacotes utilizados.
5. Explique cada configuração.

Não avance para a próxima fase.
```

---