# FinancialTransaction — Fase 17: Idempotência

> Prompt de execução para Claude Code.
>
> **Como usar:** execute este prompt dentro da raiz do repositório `FinancialTransaction`.
> Leia o `PROJECT_GUIDE.md` antes de começar. Implemente somente esta fase.
> Ao concluir, execute os testes/validações descritos, atualize a documentação quando solicitado e pare.
> Não avance automaticamente para a próxima fase.

---

# Fase 17 — Idempotência

## Objetivo

Garantir processamento único lógico.

Simular:

```text
TransactionCreated
TransactionCreated
TransactionCreated
```

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Implemente idempotência no processamento de mensagens Kafka.

Explique:

1. Por que mensagens podem ser processadas novamente.
2. At-least-once delivery.
3. Idempotência.
4. EventId.
5. Idempotency Key.
6. Processed Events.

O mesmo evento não deve causar processamento financeiro duplicado.

Crie uma estratégia persistente de idempotência.

Teste:

1. Publicar evento.
2. Processar.
3. Publicar novamente o mesmo EventId.
4. Garantir que não haja processamento duplicado.

Mostre o comportamento nos logs e traces.
```

---