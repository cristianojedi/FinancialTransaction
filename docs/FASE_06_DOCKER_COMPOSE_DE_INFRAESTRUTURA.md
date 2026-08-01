# FinancialTransaction — Fase 6: Docker Compose de infraestrutura

> Prompt de execução para Claude Code.
>
> **Como usar:** execute este prompt dentro da raiz do repositório `FinancialTransaction`.
> Leia o `PROJECT_GUIDE.md` antes de começar. Implemente somente esta fase.
> Ao concluir, execute os testes/validações descritos, atualize a documentação quando solicitado e pare.
> Não avance automaticamente para a próxima fase.

---

# Fase 6 — Docker Compose de infraestrutura

## Objetivo

Adicionar:

```text
PostgreSQL
Kafka
Kafka UI
```

Arquitetura:

```text
Docker Compose
│
├── PostgreSQL
│
├── Kafka
│
└── Kafka UI
```

### Definition of Done

- [ ] PostgreSQL;
- [ ] Kafka;
- [ ] Kafka UI;
- [ ] volumes persistentes;
- [ ] healthchecks;
- [ ] rede Docker;
- [ ] configuração documentada.

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Nesta fase crie a infraestrutura Docker Compose.

Serviços:

- PostgreSQL.
- Apache Kafka.
- Kafka UI.

Explique:

1. O que é Kafka.
2. Diferença entre Kafka e uma fila tradicional.
3. Topic.
4. Partition.
5. Offset.
6. Consumer Group.
7. Producer.
8. Consumer.

Configure:

- volumes.
- networks.
- healthchecks.
- portas.
- variáveis de ambiente.

Use uma configuração atual e compatível com Docker Compose.

Não implemente Producer.
Não implemente Consumer.
Não implemente Worker.

O objetivo é apenas subir a infraestrutura.

Forneça comandos:

docker compose up -d
docker compose ps
docker compose logs
docker compose down

Explique como acessar Kafka UI e PostgreSQL.

Não avance para a próxima fase.
```

---