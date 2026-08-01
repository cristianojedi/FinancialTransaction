# FinancialTransaction — Fase 18: Dead Letter Topic

> Prompt de execução para Claude Code.
>
> **Como usar:** execute este prompt dentro da raiz do repositório `FinancialTransaction`.
> Leia o `PROJECT_GUIDE.md` antes de começar. Implemente somente esta fase.
> Ao concluir, execute os testes/validações descritos, atualize a documentação quando solicitado e pare.
> Não avance automaticamente para a próxima fase.

---

# Fase 18 — Dead Letter Topic

## Objetivo

Adicionar tratamento de mensagens que não podem ser processadas.

Topics:

```text
financial.transactions.created
financial.transactions.failed
financial.transactions.dlq
```

Fluxo:

```text
Kafka
 │
 ▼
Worker
 │
 ├── Sucesso ──► Processed
 │
 └── Falha
       │
       ▼
     Retry
       │
       ▼
      DLQ
```

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Implemente Dead Letter Topic.

Topics:

financial.transactions.created
financial.transactions.failed
financial.transactions.dlq

Explique:

1. O que é DLQ.
2. Quando usar.
3. Diferença entre erro transitório e permanente.
4. Por que não devemos enviar tudo diretamente para DLQ.
5. Retry e DLQ.

Implemente:

- limite de tentativas;
- retry;
- DLQ;
- metadados da mensagem;
- motivo da falha.

Teste uma mensagem inválida e acompanhe:

Kafka
 -> Worker
 -> Retry
 -> DLQ

Observe o fluxo nos traces e logs.
```

---