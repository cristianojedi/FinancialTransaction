# FinancialTransaction — Fase 8: Kafka Consumer + Worker

> Prompt de execução para Claude Code.
>
> **Como usar:** execute este prompt dentro da raiz do repositório `FinancialTransaction`.
> Leia o `PROJECT_GUIDE.md` antes de começar. Implemente somente esta fase.
> Ao concluir, execute os testes/validações descritos, atualize a documentação quando solicitado e pare.
> Não avance automaticamente para a próxima fase.

---

# Fase 8 — Kafka Consumer + Worker

## Objetivo

Criar:

```text
FinancialTransaction.Worker
```

Fluxo:

```text
Kafka
 │
 ▼
Consumer
 │
 ▼
Worker
 │
 ▼
Application
 │
 ▼
PostgreSQL
```

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Nesta fase implemente o FinancialTransaction.Worker.

O Worker deve consumir:

financial.transactions.created

Explique:

1. O que é BackgroundService.
2. O que é Consumer Group.
3. Como Kafka controla Offset.
4. O que acontece quando o Worker reinicia.
5. Como tratar exceções.
6. Como evitar perder mensagens.

O Worker deve:

1. Consumir mensagem.
2. Desserializar TransactionCreated.
3. Localizar transação.
4. Alterar status para Processing.
5. Executar processamento.
6. Alterar para Processed ou Failed.

Não implemente ainda:

- OpenTelemetry.
- DLQ.
- Idempotência avançada.
- Retry avançado.

Teste:

Blazor
 -> API
 -> PostgreSQL
 -> Kafka
 -> Worker
 -> PostgreSQL

Ao final explique como validar cada etapa.
```

---