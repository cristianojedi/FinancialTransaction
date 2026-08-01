# FinancialTransaction — Fase 12: Distributed Tracing e propagação de contexto pelo Kafka

> Prompt de execução para Claude Code.
>
> **Como usar:** execute este prompt dentro da raiz do repositório `FinancialTransaction`.
> Leia o `PROJECT_GUIDE.md` antes de começar. Implemente somente esta fase.
> Ao concluir, execute os testes/validações descritos, atualize a documentação quando solicitado e pare.
> Não avance automaticamente para a próxima fase.

---

# Fase 12 — Distributed Tracing e propagação de contexto pelo Kafka

## Objetivo

Esta é uma das fases centrais do laboratório.

Agora vamos conectar os traces.

Antes:

```text
Trace A

API
 │
 └── PostgreSQL


Trace B

Kafka
 │
 └── Worker
      │
      └── PostgreSQL
```

Depois:

```text
Trace ABC123

API
 │
 ├── HTTP
 │
 ├── PostgreSQL
 │
 └── Kafka PRODUCE
          │
          │ Trace Context
          ▼
        Kafka
          │
          │ Trace Context
          ▼
    Kafka CONSUME
          │
          ├── Process Transaction
          │
          └── PostgreSQL
```

### Conceitos

Estudar:

- Distributed Tracing;
- W3C Trace Context;
- `traceparent`;
- `TraceId`;
- `SpanId`;
- `SpanContext`;
- Context Propagation;
- Inject;
- Extract;
- Producer Span;
- Consumer Span;
- Parent/Child Span;
- Links entre spans;
- Event-driven tracing.

### Objetivo técnico

A mensagem Kafka deverá transportar o contexto de trace.

Conceitualmente:

```text
API
TraceId = ABC123
     │
     │ Inject Trace Context
     ▼
Kafka Message Headers
     │
     │ Extract Trace Context
     ▼
Worker
TraceId = ABC123
```

### Definition of Done

- [ ] Producer cria Span;
- [ ] Trace Context é inserido nos headers;
- [ ] Consumer extrai Trace Context;
- [ ] Worker continua o contexto;
- [ ] API e Worker podem ser correlacionados;
- [ ] Trace completo visualizável;
- [ ] Kafka aparece no fluxo distribuído.

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Esta é uma fase crítica de Distributed Tracing.

Até agora temos:

Trace da API:

API
 -> PostgreSQL

Trace do Worker:

Kafka Consumer
 -> Worker
 -> PostgreSQL

Agora queremos propagar o contexto de tracing através do Kafka.

O fluxo final desejado é:

Blazor
 -> API
 -> PostgreSQL
 -> Kafka Producer
 -> Kafka
 -> Kafka Consumer
 -> Worker
 -> PostgreSQL

Explique profundamente:

1. O que é Distributed Tracing.
2. O que é W3C Trace Context.
3. O que é traceparent.
4. O que é TraceId.
5. O que é SpanId.
6. O que é SpanContext.
7. O que significa inject.
8. O que significa extract.
9. Como contexto é propagado através de HTTP.
10. Por que Kafka exige propagação explícita através dos headers.
11. Diferença entre Producer Span e Consumer Span.
12. Relação parent/child entre spans.
13. Quando usar Span Links em arquiteturas orientadas a eventos.

Implemente a propagação do contexto de tracing através dos headers da mensagem Kafka.

O Producer deve:

1. Obter o contexto atual.
2. Criar ou utilizar o Span de publicação.
3. Injetar o contexto nos headers Kafka.
4. Publicar a mensagem.

O Consumer deve:

1. Ler os headers Kafka.
2. Extrair o contexto.
3. Criar o Span de consumo.
4. Criar o Span de processamento.
5. Executar o processamento.
6. Persistir no PostgreSQL.

O objetivo é permitir rastrear uma operação distribuída.

IMPORTANTE:
Não adicione ainda OpenTelemetry Collector.
Não adicione ainda Grafana.
Não adicione ainda Prometheus.
Não adicione ainda Jaeger/Tempo como parte da infraestrutura definitiva.

Primeiro faça o Distributed Tracing funcionar conceitualmente.

Ao final explique como validar:

Blazor
 -> API
 -> PostgreSQL
 -> Kafka PRODUCE
 -> Kafka CONSUME
 -> Worker
 -> PostgreSQL

com o mesmo TraceId ou com a relação de contexto esperada.

Mostre também como investigar um caso em que a propagação do TraceId não funcionou.

Não avance para a próxima fase.
```

---