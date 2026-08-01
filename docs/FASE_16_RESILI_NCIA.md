# FinancialTransaction — Fase 16: Resiliência

> Prompt de execução para Claude Code.
>
> **Como usar:** execute este prompt dentro da raiz do repositório `FinancialTransaction`.
> Leia o `PROJECT_GUIDE.md` antes de começar. Implemente somente esta fase.
> Ao concluir, execute os testes/validações descritos, atualize a documentação quando solicitado e pare.
> Não avance automaticamente para a próxima fase.

---

# Fase 16 — Resiliência

## Objetivo

Estudar falhas reais.

Simular:

```text
Kafka indisponível
PostgreSQL indisponível
Worker parado
API indisponível
```

Adicionar:

- Retry;
- Timeout;
- Circuit Breaker quando fizer sentido;
- tratamento de falhas.

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Nesta fase implemente resiliência.

Explique:

1. Retry.
2. Timeout.
3. Circuit Breaker.
4. Backoff.
5. Por que retry indiscriminado pode ser perigoso.

Implemente estratégias adequadas para:

- API -> PostgreSQL.
- API -> Kafka.
- Worker -> PostgreSQL.

Simule falhas.

Documente o comportamento esperado.

Não implemente ainda DLQ.
Não implemente ainda idempotência avançada.

Ao final mostre como observar as falhas no Grafana e nos traces.
```

---