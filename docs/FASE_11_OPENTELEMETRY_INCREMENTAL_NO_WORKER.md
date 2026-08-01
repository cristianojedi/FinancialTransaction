# FinancialTransaction — Fase 11: OpenTelemetry incremental no Worker

> Prompt de execução para Claude Code.
>
> **Como usar:** execute este prompt dentro da raiz do repositório `FinancialTransaction`.
> Leia o `PROJECT_GUIDE.md` antes de começar. Implemente somente esta fase.
> Ao concluir, execute os testes/validações descritos, atualize a documentação quando solicitado e pare.
> Não avance automaticamente para a próxima fase.

---

# Fase 11 — OpenTelemetry incremental no Worker

## Objetivo

Agora instrumentar o Worker de forma independente.

Fluxo:

```text
Kafka
  │
  ▼
FinancialTransaction.Worker
  │
  ├── Consume Span
  │
  ├── Process Transaction Span
  │
  └── PostgreSQL Span
```

Nesta fase ainda não faremos a correlação entre o trace da API e o trace do Worker.

### Conceitos

Estudar:

- Worker Service;
- BackgroundService;
- Activity;
- Spans em processamento assíncrono;
- Instrumentação de operações de longa duração;
- Diferença entre trace HTTP e processamento em background.

### Definition of Done

- [ ] Worker instrumentado;
- [ ] Kafka Consumer instrumentado;
- [ ] processamento instrumentado;
- [ ] PostgreSQL instrumentado;
- [ ] Trace gerado pelo Worker;
- [ ] Trace independente da API.

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Nesta fase implemente OpenTelemetry SOMENTE no:

FinancialTransaction.Worker

O fluxo observado será:

Kafka
 -> Consumer
 -> Worker
 -> Application
 -> PostgreSQL

Explique:

1. Como instrumentar um Worker Service.
2. Como criar Activities para processamento em background.
3. Como representar o consumo de uma mensagem.
4. Como representar o processamento da transação.
5. Como instrumentar o acesso ao PostgreSQL.
6. Como diferenciar uma operação de consumo de uma operação de processamento.

Nesta fase o Worker deverá gerar seus próprios traces.

IMPORTANTE:
Ainda NÃO faça propagação do TraceId da API através do Kafka.

Ou seja, queremos inicialmente:

Trace A:
API -> PostgreSQL

Trace B:
Kafka Consumer -> Worker -> PostgreSQL

Os dois traces ainda podem ser independentes.

Não implemente ainda:

- Propagação W3C através do Kafka.
- OTel Collector.
- Grafana.
- Prometheus.
- Jaeger/Tempo.

Ao final:

1. Publique uma transação.
2. Faça o Worker processá-la.
3. Valide o trace do Worker.
4. Explique cada Span gerado.

Não avance para a próxima fase.
```

---