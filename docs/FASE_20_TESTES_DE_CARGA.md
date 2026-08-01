# FinancialTransaction — Fase 20: Testes de carga

> Prompt de execução para Claude Code.
>
> **Como usar:** execute este prompt dentro da raiz do repositório `FinancialTransaction`.
> Leia o `PROJECT_GUIDE.md` antes de começar. Implemente somente esta fase.
> Ao concluir, execute os testes/validações descritos, atualize a documentação quando solicitado e pare.
> Não avance automaticamente para a próxima fase.

---

# Fase 20 — Testes de carga

## Objetivo

Avaliar comportamento sob carga.

Cenários:

```text
100 transações
1.000 transações
10.000 transações
```

Observar:

- throughput;
- latência;
- erros;
- consumer lag;
- tempo de processamento;
- banco;
- Kafka.

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Nesta fase implemente testes de carga.

Escolha uma ferramenta adequada, como k6.

Crie cenários para:

- 100 transações.
- 1.000 transações.
- 10.000 transações.

Meça:

- requests por segundo;
- latência;
- p95;
- p99;
- erros;
- Kafka consumer lag;
- tempo de processamento.

Explique como interpretar os resultados.

Use Grafana e Prometheus para observar o comportamento.

Não faça otimizações automaticamente.

Primeiro meça.
Depois identifique gargalos.
Depois proponha melhorias.
```

---

# 8. Definition of Done global

O projeto estará concluído quando:

- [ ] Solução usa `.slnx`.
- [ ] API .NET 10 funcionando.
- [ ] Worker .NET 10 funcionando.
- [ ] Blazor + MudBlazor funcionando.
- [ ] PostgreSQL funcionando.
- [ ] EF Core funcionando.
- [ ] Kafka funcionando.
- [ ] Kafka UI funcionando.
- [ ] Producer funcionando.
- [ ] Consumer funcionando.
- [ ] Processamento assíncrono funcionando.
- [ ] OpenTelemetry configurado na API.
- [ ] OpenTelemetry configurado no Worker.
- [ ] Kafka Producer instrumentado.
- [ ] Kafka Consumer instrumentado.
- [ ] Trace distribuído funcionando.
- [ ] TraceId propagado através do Kafka.
- [ ] OpenTelemetry Collector funcionando.
- [ ] Prometheus funcionando.
- [ ] Grafana funcionando.
- [ ] Jaeger/Tempo funcionando.
- [ ] Logs estruturados funcionando.
- [ ] Logs correlacionados com TraceId.
- [ ] Retry funcionando.
- [ ] Idempotência funcionando.
- [ ] DLQ funcionando.
- [ ] Docker Compose completo funcionando.
- [ ] Testes unitários funcionando.
- [ ] Testes de integração funcionando.
- [ ] Teste end-to-end funcionando.
- [ ] Testes de carga executados.
- [ ] Documentação atualizada.

---

# 9. Cenários de falha para experimentar

Depois que o projeto estiver completo, experimente:

## 1. Kafka parado

```bash
docker compose stop kafka
```

Observar:

- API;
- logs;
- retry;
- métricas.

---

## 2. PostgreSQL parado

```bash
docker compose stop postgres
```

Observar:

- API;
- Worker;
- retry;
- traces.

---

## 3. Worker parado

```bash
docker compose stop worker
```

Criar transações.

Observar:

```text
Pending
```

Depois iniciar:

```bash
docker compose start worker
```

Observar processamento.

---

## 4. Mensagem duplicada

Publicar duas vezes o mesmo evento.

Verificar idempotência.

---

## 5. Mensagem inválida

Enviar payload inválido.

Verificar:

```text
Retry
  ↓
DLQ
```

---

## 6. Aumentar Partitions

Avaliar:

- paralelismo;
- consumer groups;
- distribuição de mensagens.

---

## 7. Executar múltiplos Workers

Executar mais de uma instância.

Observar:

```text
Consumer Group
      │
      ├── Worker 1
      ├── Worker 2
      └── Worker 3
```

---

# 10. Checklist de execução final

```text
[ ] Docker iniciado

[ ] PostgreSQL saudável
[ ] Kafka saudável
[ ] Kafka UI acessível

[ ] API iniciada
[ ] Worker iniciado
[ ] Blazor iniciado

[ ] OpenTelemetry Collector saudável
[ ] Prometheus saudável
[ ] Grafana saudável
[ ] Jaeger/Tempo saudável

[ ] Criar transação no Blazor

[ ] API recebe request
[ ] PostgreSQL salva Pending
[ ] Kafka recebe evento
[ ] Worker consome
[ ] Worker processa
[ ] PostgreSQL atualiza status

[ ] Blazor consulta status
[ ] Status Processed exibido

[ ] Trace completo disponível
[ ] TraceId correlacionado
[ ] Métricas disponíveis
[ ] Logs correlacionados
```

---

# 11. Resultado esperado

Ao final do projeto, o fluxo completo será:

```text
┌──────────────┐
│   Browser    │
└──────┬───────┘
       │
       │ HTTP
       ▼
┌──────────────┐
│    Blazor    │
│  MudBlazor   │
└──────┬───────┘
       │
       │ POST
       ▼
┌──────────────┐
│     API      │
│   .NET 10    │
└──────┬───────┘
       │
       ├───────────────┐
       │               │
       ▼               ▼
┌──────────────┐  ┌──────────────┐
│  PostgreSQL  │  │    Kafka     │
│   Pending    │  │   Producer   │
└──────────────┘  └──────┬───────┘
                         │
                         ▼
                  ┌──────────────┐
                  │    Kafka     │
                  │    Topic     │
                  └──────┬───────┘
                         │
                         ▼
                  ┌──────────────┐
                  │    Worker    │
                  │   .NET 10    │
                  └──────┬───────┘
                         │
                         ▼
                  ┌──────────────┐
                  │  PostgreSQL  │
                  │  Processed   │
                  └──────────────┘


             OBSERVABILIDADE

 API ────────────────┐
                     │
 Worker ─────────────┼──► OpenTelemetry
                     │
 Kafka ──────────────┘
                            │
                            ▼
                   OTel Collector
                     │    │    │
                     ▼    ▼    ▼
                  Traces Metrics Logs
                     │    │    │
                     ▼    ▼    ▼
                  Jaeger Prometheus
                           │
                           ▼
                         Grafana
```

---

# 12. Filosofia de implementação

A regra principal deste projeto é:

> **Implementar pouco, testar muito, observar sempre e só então avançar.**

Cada fase deve terminar com:

```text
Implementar
    ↓
Compilar
    ↓
Testar
    ↓
Observar
    ↓
Documentar
    ↓
Commit Git
    ↓
Próxima fase
```

Não avance para a próxima fase enquanto a atual não estiver funcionando.

O projeto deve ser tratado como um laboratório de arquitetura distribuída, e não como um exercício de copiar e colar código.

O objetivo final não é apenas "fazer funcionar".

O objetivo é conseguir responder:

- Onde está minha transação?
- Por que ela demorou?
- Onde ocorreu o erro?
- Kafka recebeu a mensagem?
- O Worker processou?
- O banco respondeu?
- O evento foi duplicado?
- O evento foi para DLQ?
- Qual é o TraceId?
- Qual serviço apresentou o gargalo?
- Quantas mensagens estão aguardando processamento?
- O sistema está saudável?

Ao finalizar o laboratório, você deverá ser capaz de acompanhar uma transação desde a tela Blazor até o processamento assíncrono no Worker e visualizar todo o caminho através de observabilidade distribuída.


---

# 13. Roadmap incremental de observabilidade

A evolução de observabilidade deve seguir esta ordem:

```text
API + PostgreSQL
       │
       ▼
Kafka
       │
       ▼
Worker
       │
       ▼
Fluxo completo
       │
       ▼
OpenTelemetry na API
       │
       ▼
OpenTelemetry no Worker
       │
       ▼
Instrumentação Kafka Producer/Consumer
       │
       ▼
Propagação de Trace Context pelo Kafka
       │
       ▼
OpenTelemetry Collector
       │
       ▼
Jaeger/Tempo + Prometheus
       │
       ▼
Grafana
       │
       ▼
Logs estruturados + correlação
```

A regra de aprendizado é não introduzir toda a plataforma de observabilidade de uma só vez.

Primeiro observe a API.

Depois observe o Worker.

Depois conecte os traces através do Kafka.

Só então introduza o Collector e os backends.

Dessa forma, quando algo não aparecer no trace, será possível identificar exatamente em qual camada ocorreu a quebra:

```text
HTTP
  │
  ▼
API OTel
  │
  ▼
Kafka Producer
  │
  ▼
Kafka Headers
  │
  ▼
Kafka Consumer
  │
  ▼
Worker OTel
  │
  ▼
OTel Collector
  │
  ▼
Trace Backend
  │
  ▼
Grafana
```

Essa abordagem é deliberadamente incremental e deve ser preservada durante o desenvolvimento.