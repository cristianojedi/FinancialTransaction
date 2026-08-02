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

- [x] PostgreSQL;
- [x] Kafka;
- [x] Kafka UI;
- [x] volumes persistentes;
- [x] healthchecks;
- [x] rede Docker;
- [x] configuração documentada.

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

## O que foi implementado

### Serviços no `docker-compose.yml`

O `docker-compose.yml`, que já continha o PostgreSQL desde a Fase 3, ganhou dois novos serviços:

- **`kafka`** — imagem oficial `apache/kafka:3.9.0`, rodando em modo **KRaft** (broker + controller no mesmo processo, sem Zookeeper — ver [docs/kafka.md](kafka.md#kraft-sem-zookeeper)).
- **`kafka-ui`** — imagem `provectuslabs/kafka-ui:latest`, interface web para inspecionar tópicos, partitions, mensagens e consumer groups.

Os conceitos de Kafka (Topic, Partition, Offset, Consumer Group, Producer, Consumer e a diferença para uma fila tradicional) foram documentados separadamente em [docs/kafka.md](kafka.md), para não poluir este arquivo de fase e servir de referência às Fases 7 e 8.

### Rede Docker

Todos os serviços (`postgres`, `kafka`, `kafka-ui`) foram colocados na rede `financialtransaction-network`, permitindo que se enxerguem pelo nome do serviço (ex.: `kafka:19092`) — o mesmo padrão que será usado pela Api/Worker quando forem dockerizados (Fase 19).

### Listeners do Kafka

O Kafka foi configurado com dois listeners distintos:

- `PLAINTEXT://kafka:19092` — listener **interno**, usado por outros containers na mesma rede Docker (Kafka UI hoje; Api/Worker quando forem dockerizados).
- `PLAINTEXT_HOST://localhost:9092` — listener **externo**, usado por ferramentas rodando no host (ex.: a própria Api/Worker executando localmente via `dotnet run`, fora do Docker, nas Fases 7/8).

Essa separação é necessária porque o endereço que um listener anuncia (`advertised.listeners`) precisa ser alcançável por quem está do outro lado: containers na rede Docker não resolvem `localhost` para o Kafka, e o host não resolve o hostname `kafka`.

### Volumes persistentes

- `financialtransaction-postgres-data` (já existente) — dados do PostgreSQL.
- `financialtransaction-kafka-data` (novo) — logs de partition do Kafka (`KAFKA_LOG_DIRS: /var/lib/kafka/data`), garantindo que tópicos e mensagens sobrevivam a um `docker compose down`/`up` (sem `-v`).

### Healthchecks

- `postgres` — `pg_isready` (já existente).
- `kafka` — `kafka-broker-api-versions.sh --bootstrap-server kafka:19092`, script incluído na própria imagem; falha enquanto o broker ainda não aceita conexões.
- `kafka-ui` — depende de `kafka` com `condition: service_healthy`, evitando que suba antes do broker estar pronto.

### Validação executada

```bash
docker compose up -d
docker compose ps
```

Resultado observado — os três serviços saudáveis:

```text
NAME                            STATUS
financialtransaction_postgres   Up (healthy)
financialtransaction_kafka      Up (healthy)
financialtransaction_kafka_ui   Up
```

Também foi validado manualmente, via `docker exec`, que é possível criar e listar um tópico:

```bash
docker exec financialtransaction_kafka /opt/kafka/bin/kafka-topics.sh \
  --create --topic teste.fase6 --bootstrap-server kafka:19092 --partitions 3 --replication-factor 1

docker exec financialtransaction_kafka /opt/kafka/bin/kafka-topics.sh \
  --list --bootstrap-server kafka:19092
```

E que o Kafka UI responde em `http://localhost:8080` (HTTP 200). O tópico de teste foi removido em seguida — nenhum tópico de aplicação foi criado nesta fase (Producer/Consumer ficam para as Fases 7/8).

### Comandos de operação

```bash
docker compose up -d      # sobe PostgreSQL, Kafka e Kafka UI em background
docker compose ps         # lista os containers e seu status/healthcheck
docker compose logs       # logs de todos os serviços
docker compose logs kafka # logs de um serviço específico
docker compose down       # para e remove os containers (mantém os volumes)
```

### Como acessar

- **Kafka UI**: `http://localhost:8080` — navegue até o cluster `financialtransaction` para ver tópicos, partitions, mensagens e consumer groups.
- **PostgreSQL**: `localhost:5432`, banco `financialtransaction`, usuário/senha `financialtransaction` (ver `ConnectionStrings:PostgreSql` em `src/FinancialTransaction.Api/appsettings.json`). Qualquer cliente (`psql`, DBeaver, Azure Data Studio, extensão do editor) pode se conectar com esses dados.
- **Kafka (fora do Docker)**: `localhost:9092`, para uma futura Api/Worker rodando localmente via `dotnet run` (Fases 7/8) ou para ferramentas de linha de comando no host.

### Fora do escopo desta fase (propositalmente)

- Nenhum tópico de aplicação foi criado — isso é da Fase 7.
- Nenhum Producer, Consumer ou Worker foi implementado.
- OpenTelemetry, Prometheus, Grafana e Jaeger/Tempo ainda não fazem parte da infraestrutura (entram a partir da Fase 13/14).

---