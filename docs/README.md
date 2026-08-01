# FinancialTransaction — Prompts por Fase

Esta pasta contém os prompts separados para execução incremental no Claude Code.

## Ordem recomendada

Execute uma fase por vez:

1. `FASE_00_PREPARACAO_DO_AMBIENTE.md`
2. `FASE_01_ESTRUTURA_DA_SOLUCAO.md`
3. `FASE_02_DOMINIO_FINANCEIRO.md`
4. `FASE_03_POSTGRESQL_EF_CORE.md`
5. `FASE_04_API_REST.md`
6. `FASE_05_BLAZOR_MUDBLAZOR.md`
7. `FASE_06_KAFKA_INFRAESTRUTURA.md`
8. `FASE_07_KAFKA_PRODUCER.md`
9. `FASE_08_KAFKA_CONSUMER_WORKER.md`
10. `FASE_09_FLUXO_FINANCEIRO_COMPLETO.md`
11. `FASE_10_OPENTELEMETRY_INCREMENTAL_NA_API.md`
12. `FASE_11_OPENTELEMETRY_INCREMENTAL_NO_WORKER.md`
13. `FASE_12_DISTRIBUTED_TRACING_E_PROPAGACAO_DE_CONTEXTO_PELO_KAFKA.md`
14. `FASE_13_OPENTELEMETRY_COLLECTOR.md`
15. `FASE_14_JAEGER_TEMPO_PROMETHEUS_GRAFANA.md`
16. `FASE_15_LOGS_ESTRUTURADOS.md`
17. `FASE_16_RESILIENCIA.md`
18. `FASE_17_IDEMPOTENCIA.md`
19. `FASE_18_DEAD_LETTER_TOPIC.md`
20. `FASE_19_DOCKERIZACAO_COMPLETA.md`
21. `FASE_20_TESTES_DE_CARGA.md`

## Fluxo de execução

```text
Abrir prompt da fase
       ↓
Colar/enviar para Claude Code
       ↓
Claude implementa
       ↓
Executar testes
       ↓
Validar Definition of Done
       ↓
Fazer commit Git
       ↓
Só então executar próxima fase
```

## Regra importante

Não peça ao Claude Code para executar todas as fases de uma vez.

A ideia do laboratório é:

**implementar → testar → observar → documentar → commit → próxima fase**

O `PROJECT_GUIDE.md` permanece como documentação mestre. Estes arquivos são os prompts operacionais separados para execução.
