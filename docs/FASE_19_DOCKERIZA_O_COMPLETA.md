# FinancialTransaction — Fase 19: Dockerização completa

> Prompt de execução para Claude Code.
>
> **Como usar:** execute este prompt dentro da raiz do repositório `FinancialTransaction`.
> Leia o `PROJECT_GUIDE.md` antes de começar. Implemente somente esta fase.
> Ao concluir, execute os testes/validações descritos, atualize a documentação quando solicitado e pare.
> Não avance automaticamente para a próxima fase.

---

# Fase 19 — Dockerização completa

## Objetivo

Executar tudo via Docker Compose.

Serviços:

```text
FinancialTransaction.Web
FinancialTransaction.Api
FinancialTransaction.Worker
PostgreSQL
Kafka
Kafka UI
OpenTelemetry Collector
Prometheus
Grafana
Jaeger/Tempo
```

Comando:

```bash
docker compose up -d
```

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Agora dockerize toda a solução.

Serviços:

- FinancialTransaction.Web
- FinancialTransaction.Api
- FinancialTransaction.Worker
- PostgreSQL
- Kafka
- Kafka UI
- OpenTelemetry Collector
- Prometheus
- Grafana
- Jaeger ou Tempo

Crie Dockerfiles adequados.

Crie Docker Compose completo.

Configure:

- networks;
- volumes;
- healthchecks;
- depends_on;
- variáveis de ambiente;
- connection strings;
- URLs internas.

Explique a diferença entre:

localhost

e nomes de serviços Docker.

O ambiente deve iniciar com:

docker compose up -d

Valide:

Blazor
 -> API
 -> PostgreSQL
 -> Kafka
 -> Worker
 -> PostgreSQL
 -> OpenTelemetry
 -> Grafana/Jaeger

Documente todos os endpoints e portas.
```

---