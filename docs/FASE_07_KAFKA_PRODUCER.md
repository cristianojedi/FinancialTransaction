# FinancialTransaction — Fase 7: Kafka Producer

> Prompt de execução para Claude Code.
>
> **Como usar:** execute este prompt dentro da raiz do repositório `FinancialTransaction`.
> Leia o `PROJECT_GUIDE.md` antes de começar. Implemente somente esta fase.
> Ao concluir, execute os testes/validações descritos, atualize a documentação quando solicitado e pare.
> Não avance automaticamente para a próxima fase.

---

# Fase 7 — Kafka Producer

## Objetivo

Alterar o fluxo:

```text
API
 │
 ├── PostgreSQL
 │
 └── Kafka
       │
       ▼
TransactionCreated
```

Topic:

```text
financial.transactions.created
```

### Definition of Done

- [ ] Producer;
- [ ] topic;
- [ ] evento;
- [ ] serialização;
- [ ] publicação;
- [ ] mensagem visualizada no Kafka UI.

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Nesta fase implemente somente o Kafka Producer.

Evento:

TransactionCreated

Topic:

financial.transactions.created

O fluxo será:

POST /api/transactions
        |
        +--> PostgreSQL
        |
        +--> Kafka Producer
                 |
                 v
       financial.transactions.created

Explique:

1. O que é Producer.
2. O que é Topic.
3. Como Kafka serializa mensagens.
4. Como escolher a chave da mensagem.
5. O que acontece quando Kafka está indisponível.
6. Como lidar com falha de publicação.

Implemente:

- abstração de mensageria;
- producer Kafka;
- serialização JSON;
- configuração;
- publicação do evento.

Não implemente Consumer.
Não implemente Worker.
Não implemente OpenTelemetry.

Ao final:

1. Suba Kafka.
2. Crie ou valide o topic.
3. Execute POST.
4. Verifique a mensagem no Kafka UI.

Explique todo o fluxo.
```

---