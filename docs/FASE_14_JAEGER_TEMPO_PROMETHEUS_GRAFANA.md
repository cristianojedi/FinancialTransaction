# FinancialTransaction — Fase 14: Jaeger/Tempo + Prometheus + Grafana

> Prompt de execução para Claude Code.
>
> **Como usar:** execute este prompt dentro da raiz do repositório `FinancialTransaction`.
> Leia o `PROJECT_GUIDE.md` antes de começar. Implemente somente esta fase.
> Ao concluir, execute os testes/validações descritos, atualize a documentação quando solicitado e pare.
> Não avance automaticamente para a próxima fase.

---

# Fase 14 — Jaeger/Tempo + Prometheus + Grafana

## Objetivo

Adicionar os backends e a visualização da observabilidade.

Arquitetura:

```text
API ───────────────┐
                   │
Worker ────────────┼──► OpenTelemetry Collector
                   │
                   ▼
              ┌───────────┐
              │           │
              ▼           ▼
           Traces      Metrics
              │           │
              ▼           ▼
         Jaeger/Tempo  Prometheus
              │           │
              └─────┬─────┘
                    ▼
                 Grafana
```

### Definition of Done

- [ ] Traces disponíveis;
- [ ] Metrics disponíveis;
- [ ] Grafana configurado;
- [ ] Datasources configurados;
- [ ] Trace completo visualizável;
- [ ] dashboards iniciais.

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Agora adicione os componentes de observabilidade visual.

Tecnologias:

- Jaeger ou Grafana Tempo.
- Prometheus.
- Grafana.

Explique:

1. O que é backend de tracing.
2. O que é backend de métricas.
3. O que é Grafana.
4. O papel do Collector entre aplicações e backends.
5. Como Grafana consulta datasources.

Configure:

OpenTelemetry Collector
 -> Jaeger/Tempo

OpenTelemetry Collector
 -> Prometheus, conforme arquitetura de métricas escolhida

Grafana
 -> Jaeger/Tempo
 -> Prometheus

Crie dashboards para:

- HTTP requests.
- HTTP duration.
- HTTP errors.
- Transaction processing.
- Worker processing.
- Kafka messages.
- Kafka consumer lag, se disponível.
- Processing errors.

Crie também uma visualização de trace distribuído.

O trace deverá permitir investigar:

Blazor
 -> API
 -> PostgreSQL
 -> Kafka Producer
 -> Kafka Consumer
 -> Worker
 -> PostgreSQL

Explique como localizar um TraceId e investigar uma transação problemática.
```

---