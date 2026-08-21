# FinancialTransaction — PROJECT_GUIDE

> Guia de implementação incremental de um laboratório de arquitetura distribuída com .NET 10, Blazor, MudBlazor, PostgreSQL, Kafka, OpenTelemetry, Prometheus, Grafana, Jaeger/Tempo e Docker Compose.

---

## 1. Objetivo do projeto

Este projeto tem como objetivo criar, de forma incremental e didática, uma aplicação que simula o processamento assíncrono de transações financeiras fictícias.

A proposta não é construir um sistema bancário real. O foco é estudar, na prática:

- .NET 10;
- Clean Architecture;
- DDD pragmático;
- ASP.NET Core Web API;
- Blazor + MudBlazor;
- Entity Framework Core;
- PostgreSQL;
- Apache Kafka;
- Producer e Consumer;
- Worker Service;
- processamento assíncrono;
- OpenTelemetry;
- Distributed Tracing;
- Trace ID e Span ID;
- propagação de contexto através do Kafka;
- métricas;
- logs estruturados;
- Prometheus;
- Grafana;
- Jaeger ou Grafana Tempo;
- OpenTelemetry Collector;
- resiliência;
- retry;
- idempotência;
- Dead Letter Topic;
- Docker;
- Docker Compose;
- testes de integração;
- testes de carga.

A implementação será incremental. Ao final de cada fase, o sistema deverá estar executável e testável.

---

# 2. Visão geral da arquitetura

A arquitetura final desejada é:

```text
                           ┌──────────────────────┐
                           │   Financial Web      │
                           │  Blazor + MudBlazor  │
                           └──────────┬───────────┘
                                      │ HTTP
                                      ▼
                           ┌──────────────────────┐
                           │  Financial API       │
                           │      .NET 10         │
                           └──────────┬───────────┘
                                      │
                         ┌────────────┴─────────────┐
                         │                          │
                         ▼                          ▼
                  ┌─────────────┐           ┌─────────────┐
                  │ PostgreSQL  │           │    Kafka    │
                  │             │           │             │
                  └─────────────┘           └──────┬──────┘
                                                   │
                                                   │ Consume
                                                   ▼
                                         ┌──────────────────────┐
                                         │ Financial Worker     │
                                         │      .NET 10         │
                                         └──────────┬───────────┘
                                                    │
                                                    ▼
                                             ┌─────────────┐
                                             │ PostgreSQL  │
                                             └─────────────┘
```

---

# 3. Fluxo de negócio

O fluxo principal será:

```text
Usuário
   │
   │ Cria transação
   ▼
Blazor + MudBlazor
   │
   │ HTTP POST
   ▼
FinancialTransaction.Api
   │
   ├── Valida requisição
   │
   ├── Persiste transação como Pending
   │
   └── Publica TransactionCreated
              │
              ▼
            Kafka
              │
              │ Consume
              ▼
FinancialTransaction.Worker
              │
              ├── Carrega transação
              ├── Valida conta origem
              ├── Valida conta destino
              ├── Valida saldo
              ├── Processa transação
              │
              ▼
          PostgreSQL
              │
              ▼
      Processed ou Failed
```

O status poderá evoluir:

```text
Pending
   │
   ▼
Processing
   │
   ├──────────────► Processed
   │
   └──────────────► Failed
```

---

# 4. Arquitetura de observabilidade

A arquitetura final de observabilidade será:

```text
                 ┌──────────────────┐
                 │ Financial API    │
                 │                  │
                 │ HTTP + EF + Kafka│
                 └────────┬─────────┘
                          │
                          │ OTLP
                          ▼
                 ┌──────────────────┐
                 │ OpenTelemetry    │
                 │ Collector        │
                 └───────┬──────────┘
                         │
             ┌───────────┼────────────┐
             │           │            │
             ▼           ▼            ▼
          Traces      Metrics       Logs
             │           │            │
             ▼           ▼            ▼
        Jaeger/Tempo  Prometheus    Grafana
```

O objetivo é conseguir visualizar uma operação distribuída:

```text
TraceId: ABC123

POST /api/transactions
        │
        ├── HTTP Request
        │
        ├── PostgreSQL INSERT
        │
        ├── Kafka PRODUCE
        │
        └──────────────► Kafka CONSUME
                              │
                              ├── Process Transaction
                              │
                              └── PostgreSQL UPDATE
```

---

# 5. Estrutura final da solução

A solução deverá utilizar o formato moderno `.slnx`:

```text
FinancialTransaction/
│
├── FinancialTransaction.slnx
│
├── src/
│   ├── FinancialTransaction.Api/
│   ├── FinancialTransaction.Application/
│   ├── FinancialTransaction.Domain/
│   ├── FinancialTransaction.Infrastructure/
│   ├── FinancialTransaction.Worker/
│   └── FinancialTransaction.Web/
│
├── tests/
│   ├── FinancialTransaction.UnitTests/
│   └── FinancialTransaction.IntegrationTests/
│
├── infrastructure/
│   └── docker/
│       ├── kafka/
│       └── observability/
│
├── docs/
│   ├── architecture.md
│   ├── kafka.md
│   ├── observability.md
│   └── troubleshooting.md
│
├── docker-compose.infrastructure.yml
├── docker-compose.observability.yml
├── docker-compose.yml
│
└── PROJECT_GUIDE.md
```

---

# 6. Estratégia de desenvolvimento

O projeto será desenvolvido em três níveis.

## Nível 1 — Desenvolvimento local

Infraestrutura em Docker:

```text
PostgreSQL
Kafka
Kafka UI
```

Aplicações executando localmente:

```text
FinancialTransaction.Api
FinancialTransaction.Worker
FinancialTransaction.Web
```

Este será o ambiente principal para desenvolvimento.

Vantagens:

- breakpoints;
- debugging;
- Hot Reload quando disponível;
- alteração rápida de código;
- inspeção de exceções.

---

## Nível 2 — Infraestrutura + observabilidade

Docker:

```text
PostgreSQL
Kafka
Kafka UI
OpenTelemetry Collector
Prometheus
Grafana
Jaeger ou Tempo
```

Aplicações locais:

```text
API
Worker
Blazor
```

---

## Nível 3 — Tudo em Docker Compose

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

Execução:

```bash
docker compose up -d
```

---

# 7. Fases do projeto

## Fase 0 — Preparação do ambiente

### Objetivo

Preparar o ambiente de desenvolvimento.

### Tecnologias

- .NET 10 SDK;
- Docker;
- Docker Compose;
- Git;
- IDE;
- Postman ou Bruno;
- navegador.

### Teste

Executar:

```bash
dotnet --version
docker --version
docker compose version
git --version
```

### Definition of Done

- [ ] .NET 10 instalado;
- [ ] Docker funcionando;
- [ ] Docker Compose funcionando;
- [ ] Git configurado;
- [ ] IDE configurada.

### Prompt para IA

```text
Atue como um arquiteto de software especialista em .NET 10.

Estou iniciando um projeto chamado FinancialTransaction, cujo objetivo é estudar arquitetura distribuída, mensageria, observabilidade e Docker.

Nesta fase, quero apenas preparar e validar o ambiente de desenvolvimento.

Tecnologias planejadas:

- .NET 10
- ASP.NET Core
- Blazor
- MudBlazor
- Entity Framework Core
- PostgreSQL
- Apache Kafka
- OpenTelemetry
- Prometheus
- Grafana
- Jaeger ou Grafana Tempo
- Docker
- Docker Compose

Explique detalhadamente:

1. O papel de cada tecnologia no projeto.
2. Quais ferramentas precisam ser instaladas localmente.
3. Como verificar as versões.
4. Como validar o Docker.
5. Como validar o Docker Compose.

Não crie ainda código da aplicação.
Não implemente Kafka.
Não implemente PostgreSQL.
Não implemente observabilidade.

O objetivo desta fase é somente validar o ambiente.
```

---

# Fase 1 — Estrutura da solução

## Objetivo

Criar a solução utilizando `FinancialTransaction.slnx`.

Projetos:

```text
FinancialTransaction.Api
FinancialTransaction.Application
FinancialTransaction.Domain
FinancialTransaction.Infrastructure
FinancialTransaction.Worker
FinancialTransaction.Web
```

Testes:

```text
FinancialTransaction.UnitTests
FinancialTransaction.IntegrationTests
```

### Arquitetura

```text
                 FinancialTransaction.Web
                         │
                         ▼
                 FinancialTransaction.Api
                         │
                         ▼
                FinancialTransaction.Application
                         │
                         ▼
                  FinancialTransaction.Domain
                         ▲
                         │
             FinancialTransaction.Infrastructure


                 FinancialTransaction.Worker
                         │
                         ▼
                FinancialTransaction.Application
                         │
                         ▼
                  FinancialTransaction.Domain
```

### Objetivo arquitetural

A camada `Domain` não deve depender de infraestrutura.

A camada `Application` concentra casos de uso.

A `Infrastructure` implementa persistência e integrações.

A `Api` expõe HTTP.

O `Worker` processa mensagens assíncronas.

O `Web` é a interface do usuário.

### Teste

```bash
dotnet build FinancialTransaction.slnx
```

### Definition of Done

- [ ] `.slnx` criado;
- [ ] projetos criados;
- [ ] referências corretas;
- [ ] solução compilando;
- [ ] testes executando.

### Prompt para IA

```text
Atue como arquiteto especialista em .NET 10 e Clean Architecture.

Crie a estrutura inicial de uma solução chamada FinancialTransaction.

IMPORTANTE:
A solução deve utilizar o formato moderno:

FinancialTransaction.slnx

Não utilize FinancialTransaction.sln.

Projetos:

src/
- FinancialTransaction.Api
- FinancialTransaction.Application
- FinancialTransaction.Domain
- FinancialTransaction.Infrastructure
- FinancialTransaction.Worker
- FinancialTransaction.Web

tests/
- FinancialTransaction.UnitTests
- FinancialTransaction.IntegrationTests

Explique detalhadamente:

1. Por que cada projeto existe.
2. Qual responsabilidade cada projeto terá.
3. Como as dependências devem ser organizadas.
4. Por que Domain não deve depender de Infrastructure.
5. Como Worker e API compartilham Application e Domain.

Crie apenas a estrutura inicial.

Não implemente banco.
Não implemente Kafka.
Não implemente OpenTelemetry.
Não implemente regras de negócio.

Ao final:

1. Mostre a árvore de diretórios.
2. Execute ou indique o comando para compilar.
3. Execute ou indique o comando para rodar os testes.
4. Explique o resultado esperado.

Não avance para a próxima fase.
```

---

# Fase 2 — Domínio financeiro

## Objetivo

Criar o modelo de domínio.

Entidades:

```text
Account
FinancialTransaction
```

Enum:

```text
TransactionStatus
```

Eventos:

```text
TransactionCreated
TransactionProcessed
TransactionFailed
```

### Testes

Criar testes unitários para:

- valor maior que zero;
- conta origem diferente da conta destino;
- transação iniciando como Pending;
- transição de estados válida;
- transição de estados inválida.

### Definition of Done

- [ ] domínio implementado;
- [ ] invariantes implementadas;
- [ ] testes unitários;
- [ ] nenhum acesso a banco;
- [ ] nenhum Kafka.

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

A solução utiliza:

FinancialTransaction.slnx

Projetos:

- Api
- Application
- Domain
- Infrastructure
- Worker
- Web
- UnitTests
- IntegrationTests

Nesta fase implemente somente o domínio financeiro.

Crie:

- Account
- FinancialTransaction
- TransactionStatus
- TransactionCreated
- TransactionProcessed
- TransactionFailed

Explique antes de implementar:

1. O que é uma entidade.
2. O que é uma Value Object, caso seja utilizada.
3. O que é uma regra de negócio.
4. Por que regras de domínio devem ficar no Domain.
5. Como representar estados de uma transação.

Regras mínimas:

- Valor deve ser maior que zero.
- Conta origem e destino devem ser diferentes.
- Nova transação inicia como Pending.
- Uma transação Pending pode ir para Processing.
- Processing pode ir para Processed ou Failed.
- Estados finais não podem voltar para Pending.

Crie testes unitários.

Não implemente:

- PostgreSQL.
- EF Core.
- Kafka.
- API.
- Worker.
- OpenTelemetry.

Ao final, explique cada classe criada e mostre como executar os testes.
```

---

# Fase 3 — PostgreSQL + EF Core

## Objetivo

Adicionar persistência.

Infraestrutura Docker:

```text
PostgreSQL
```

Fluxo:

```text
API
 │
 ▼
Application
 │
 ▼
Infrastructure
 │
 ▼
EF Core
 │
 ▼
PostgreSQL
```

### Teste

Criar uma transação e verificar no PostgreSQL.

### Definition of Done

- [ ] PostgreSQL em Docker;
- [ ] EF Core configurado;
- [ ] migrations;
- [ ] banco criado;
- [ ] persistência funcionando;
- [ ] integração testada.

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Nesta fase implemente somente persistência com PostgreSQL e Entity Framework Core.

Utilize Docker Compose para PostgreSQL.

Explique:

1. O que é DbContext.
2. O que é Migration.
3. Por que a configuração do EF Core fica em Infrastructure.
4. Como Application acessa persistência sem conhecer detalhes do banco.
5. Como configurar connection string.
6. Como executar migrations.

Implemente:

- DbContext.
- Configurações das entidades.
- Repositório ou abstração de persistência adequada.
- Migrations.
- Docker Compose do PostgreSQL.

Não implemente Kafka.
Não implemente Worker.
Não implemente OpenTelemetry.
Não implemente Blazor.

Crie testes de integração para validar persistência.

Ao final explique:

- Como subir PostgreSQL.
- Como criar banco.
- Como aplicar migration.
- Como verificar dados.
- Como executar testes.

Não avance para Kafka.
```

---

# Fase 4 — API REST

## Objetivo

Criar a API HTTP.

Endpoints:

```http
POST /api/transactions
GET /api/transactions/{id}
GET /api/transactions
```

Fluxo:

```text
HTTP
 │
 ▼
Controller/Endpoint
 │
 ▼
Application
 │
 ▼
Domain
 │
 ▼
Infrastructure
 │
 ▼
PostgreSQL
```

### Teste

Swagger/Postman.

### Definition of Done

- [ ] POST funcionando;
- [ ] GET por ID;
- [ ] GET lista;
- [ ] validação;
- [ ] Swagger;
- [ ] testes.

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Nesta fase implemente a API REST usando ASP.NET Core .NET 10.

Endpoints:

POST /api/transactions
GET /api/transactions/{id}
GET /api/transactions

Explique:

1. Diferença entre Controller e Minimal API.
2. Como separar API de Application.
3. DTOs.
4. Validação.
5. Status HTTP.
6. Como tratar erros.

O POST deve:

1. Receber conta origem.
2. Receber conta destino.
3. Receber valor.
4. Validar.
5. Criar FinancialTransaction.
6. Persistir como Pending.
7. Retornar o ID e status.

Nesta fase NÃO publique Kafka.

Não implemente Worker.
Não implemente OpenTelemetry.

Crie testes unitários e de integração.

Ao final forneça exemplos de requisições HTTP e respostas esperadas.
```

---

# Fase 5 — Blazor + MudBlazor

## Objetivo

Criar uma interface simples para iniciar o fluxo.

Tela:

```text
┌───────────────────────────────────────────────┐
│       PROCESSAMENTO DE TRANSAÇÃO              │
│                                               │
│ Conta origem                                  │
│ [ ACC-001                              ]      │
│                                               │
│ Conta destino                                 │
│ [ ACC-002                              ]      │
│                                               │
│ Valor                                         │
│ [ R$ 1.500,00                          ]      │
│                                               │
│       [ CRIAR TRANSAÇÃO ]                     │
│                                               │
│ Status: Pending                               │
└───────────────────────────────────────────────┘
```

Fluxo:

```text
Blazor
  │
  │ HTTP
  ▼
API
  │
  ▼
PostgreSQL
```

### Definition of Done

- [ ] Blazor funcionando;
- [ ] MudBlazor configurado;
- [ ] formulário;
- [ ] validação;
- [ ] chamada HTTP;
- [ ] exibição de resultado.

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Nesta fase implemente somente o frontend Blazor com MudBlazor.

Projeto:

FinancialTransaction.Web

Crie uma tela para criar uma transação financeira.

Campos:

- Conta origem.
- Conta destino.
- Valor.

Utilize componentes MudBlazor.

A tela deve:

1. Validar campos.
2. Validar valor maior que zero.
3. Impedir contas iguais.
4. Chamar POST /api/transactions.
5. Exibir o ID retornado.
6. Exibir o status Pending.
7. Exibir mensagens de erro amigáveis.
8. Ter estado de carregamento.

Explique:

1. Como Blazor chama uma API.
2. Como configurar HttpClient.
3. Como tratar erros HTTP.
4. Como organizar componentes.
5. Como usar MudBlazor.

Não implemente Kafka.
Não implemente Worker.
Não implemente OpenTelemetry.

O objetivo é criar o primeiro fluxo end-to-end:

Blazor -> API -> PostgreSQL.

Ao final forneça os passos para executar Web e API simultaneamente.
```

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

# Fase 9 — Fluxo financeiro completo

## Objetivo

Conectar tudo.

```text
Blazor
  │
  ▼
API
  │
  ├── PostgreSQL
  │
  └── Kafka
       │
       ▼
     Worker
       │
       ▼
   PostgreSQL
```

### Prompt para IA

```text
Agora integre todas as partes já implementadas.

Fluxo:

Blazor
 -> API
 -> PostgreSQL
 -> Kafka
 -> Worker
 -> PostgreSQL

O frontend deve permitir:

1. Criar transação.
2. Receber ID.
3. Consultar status.
4. Atualizar status visualmente.

Implemente polling simples para consultar o status.

Explique:

1. Processamento síncrono.
2. Processamento assíncrono.
3. Eventual consistency.
4. Por que a resposta inicial pode ser Pending.
5. Por que o resultado final não está disponível imediatamente.

Não implemente OpenTelemetry ainda.

Crie um teste end-to-end do fluxo completo.
```

---

# Fase 10 — OpenTelemetry incremental na API

## Objetivo

Adicionar OpenTelemetry primeiro somente à API.

A ideia é observar o fluxo síncrono:

```text
Blazor
  │
  ▼
FinancialTransaction.Api
  │
  ├── HTTP Span
  │
  ├── Application Span
  │
  └── PostgreSQL Span
```

Nesta fase ainda não vamos rastrear o Worker nem a propagação através do Kafka.

### Conceitos

Estudar:

- Observabilidade;
- Telemetria;
- Traces;
- Spans;
- TraceId;
- SpanId;
- Activity;
- Instrumentação automática;
- Instrumentação manual;
- Exportadores;
- OTLP.

### Definition of Done

- [x] ASP.NET Core instrumentado;
- [x] HTTP instrumentado;
- [x] EF Core instrumentado;
- [x] TraceId disponível;
- [x] Spans gerados;
- [x] Telemetria exportada;
- [x] API observável independentemente do Kafka.

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Nesta fase implemente OpenTelemetry de forma incremental.

IMPORTANTE:
Instrumente SOMENTE a FinancialTransaction.Api.

O fluxo observado será:

Blazor
 -> FinancialTransaction.Api
 -> Application
 -> PostgreSQL

Instrumente:

- ASP.NET Core.
- HTTP.
- EF Core.

Explique detalhadamente:

1. O que é observabilidade.
2. O que é OpenTelemetry.
3. O que é Trace.
4. O que é Span.
5. O que é TraceId.
6. O que é SpanId.
7. O que é Activity no ecossistema .NET.
8. Diferença entre instrumentação automática e manual.
9. Como o OpenTelemetry coleta os dados.
10. O que é OTLP.

Configure OpenTelemetry de maneira adequada para .NET 10.

Nesta fase NÃO implemente:

- OpenTelemetry no Worker.
- Propagação de contexto através do Kafka.
- OpenTelemetry Collector.
- Grafana.
- Prometheus.
- Jaeger/Tempo.

O objetivo é conseguir observar uma requisição HTTP da API e suas operações de banco.

Ao final:

1. Execute uma transação.
2. Mostre como validar que um Trace foi criado.
3. Explique como identificar HTTP e PostgreSQL dentro do trace.
4. Documente os pacotes utilizados.
5. Explique cada configuração.

Não avance para a próxima fase.
```

---

# Fase 11 — OpenTelemetry incremental no Worker

## Objetivo

Agora instrumentar o Worker de forma independente.

Fluxo:

```text
Kafka
  │
  ▼
FinancialTransaction.Worker
  │
  ├── Consume Span
  │
  ├── Process Transaction Span
  │
  └── PostgreSQL Span
```

Nesta fase ainda não faremos a correlação entre o trace da API e o trace do Worker.

### Conceitos

Estudar:

- Worker Service;
- BackgroundService;
- Activity;
- Spans em processamento assíncrono;
- Instrumentação de operações de longa duração;
- Diferença entre trace HTTP e processamento em background.

### Definition of Done

- [x] Worker instrumentado;
- [x] Kafka Consumer instrumentado;
- [x] processamento instrumentado;
- [x] PostgreSQL instrumentado;
- [x] Trace gerado pelo Worker;
- [x] Trace independente da API.

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Nesta fase implemente OpenTelemetry SOMENTE no:

FinancialTransaction.Worker

O fluxo observado será:

Kafka
 -> Consumer
 -> Worker
 -> Application
 -> PostgreSQL

Explique:

1. Como instrumentar um Worker Service.
2. Como criar Activities para processamento em background.
3. Como representar o consumo de uma mensagem.
4. Como representar o processamento da transação.
5. Como instrumentar o acesso ao PostgreSQL.
6. Como diferenciar uma operação de consumo de uma operação de processamento.

Nesta fase o Worker deverá gerar seus próprios traces.

IMPORTANTE:
Ainda NÃO faça propagação do TraceId da API através do Kafka.

Ou seja, queremos inicialmente:

Trace A:
API -> PostgreSQL

Trace B:
Kafka Consumer -> Worker -> PostgreSQL

Os dois traces ainda podem ser independentes.

Não implemente ainda:

- Propagação W3C através do Kafka.
- OTel Collector.
- Grafana.
- Prometheus.
- Jaeger/Tempo.

Ao final:

1. Publique uma transação.
2. Faça o Worker processá-la.
3. Valide o trace do Worker.
4. Explique cada Span gerado.

Não avance para a próxima fase.
```

---

# Fase 12 — Distributed Tracing e propagação de contexto pelo Kafka

## Objetivo

Esta é uma das fases centrais do laboratório.

Agora vamos conectar os traces.

Antes:

```text
Trace A

API
 │
 └── PostgreSQL


Trace B

Kafka
 │
 └── Worker
      │
      └── PostgreSQL
```

Depois:

```text
Trace ABC123

API
 │
 ├── HTTP
 │
 ├── PostgreSQL
 │
 └── Kafka PRODUCE
          │
          │ Trace Context
          ▼
        Kafka
          │
          │ Trace Context
          ▼
    Kafka CONSUME
          │
          ├── Process Transaction
          │
          └── PostgreSQL
```

### Conceitos

Estudar:

- Distributed Tracing;
- W3C Trace Context;
- `traceparent`;
- `TraceId`;
- `SpanId`;
- `SpanContext`;
- Context Propagation;
- Inject;
- Extract;
- Producer Span;
- Consumer Span;
- Parent/Child Span;
- Links entre spans;
- Event-driven tracing.

### Objetivo técnico

A mensagem Kafka deverá transportar o contexto de trace.

Conceitualmente:

```text
API
TraceId = ABC123
     │
     │ Inject Trace Context
     ▼
Kafka Message Headers
     │
     │ Extract Trace Context
     ▼
Worker
TraceId = ABC123
```

### Definition of Done

- [x] Producer cria Span;
- [x] Trace Context é inserido nos headers;
- [x] Consumer extrai Trace Context;
- [x] Worker continua o contexto;
- [x] API e Worker podem ser correlacionados;
- [x] Trace completo visualizável;
- [x] Kafka aparece no fluxo distribuído.

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Esta é uma fase crítica de Distributed Tracing.

Até agora temos:

Trace da API:

API
 -> PostgreSQL

Trace do Worker:

Kafka Consumer
 -> Worker
 -> PostgreSQL

Agora queremos propagar o contexto de tracing através do Kafka.

O fluxo final desejado é:

Blazor
 -> API
 -> PostgreSQL
 -> Kafka Producer
 -> Kafka
 -> Kafka Consumer
 -> Worker
 -> PostgreSQL

Explique profundamente:

1. O que é Distributed Tracing.
2. O que é W3C Trace Context.
3. O que é traceparent.
4. O que é TraceId.
5. O que é SpanId.
6. O que é SpanContext.
7. O que significa inject.
8. O que significa extract.
9. Como contexto é propagado através de HTTP.
10. Por que Kafka exige propagação explícita através dos headers.
11. Diferença entre Producer Span e Consumer Span.
12. Relação parent/child entre spans.
13. Quando usar Span Links em arquiteturas orientadas a eventos.

Implemente a propagação do contexto de tracing através dos headers da mensagem Kafka.

O Producer deve:

1. Obter o contexto atual.
2. Criar ou utilizar o Span de publicação.
3. Injetar o contexto nos headers Kafka.
4. Publicar a mensagem.

O Consumer deve:

1. Ler os headers Kafka.
2. Extrair o contexto.
3. Criar o Span de consumo.
4. Criar o Span de processamento.
5. Executar o processamento.
6. Persistir no PostgreSQL.

O objetivo é permitir rastrear uma operação distribuída.

IMPORTANTE:
Não adicione ainda OpenTelemetry Collector.
Não adicione ainda Grafana.
Não adicione ainda Prometheus.
Não adicione ainda Jaeger/Tempo como parte da infraestrutura definitiva.

Primeiro faça o Distributed Tracing funcionar conceitualmente.

Ao final explique como validar:

Blazor
 -> API
 -> PostgreSQL
 -> Kafka PRODUCE
 -> Kafka CONSUME
 -> Worker
 -> PostgreSQL

com o mesmo TraceId ou com a relação de contexto esperada.

Mostre também como investigar um caso em que a propagação do TraceId não funcionou.

Não avance para a próxima fase.
```

---

# Fase 13 — OpenTelemetry Collector

## Objetivo

Depois que API, Worker e propagação distribuída estiverem funcionando, introduzir o OpenTelemetry Collector.

Arquitetura:

```text
FinancialTransaction.Api
          │
          │ OTLP
          ▼
┌───────────────────────┐
│ OpenTelemetry         │
│ Collector              │
└───────────┬───────────┘
            │
            │ OTLP
            ▼
       Observability


FinancialTransaction.Worker
          │
          │ OTLP
          ▼
┌───────────────────────┐
│ OpenTelemetry         │
│ Collector              │
└───────────┬───────────┘
            │
            ▼
       Observability
```

### Conceitos

Estudar:

- OTLP;
- Collector;
- Receiver;
- Processor;
- Exporter;
- Pipeline;
- Batch Processor;
- Resource Attributes;
- Environment Attributes.

### Definition of Done

- [x] Collector em Docker;
- [x] API envia telemetria;
- [x] Worker envia telemetria;
- [x] Collector recebe;
- [x] pipeline configurado;
- [x] configuração documentada.

> Implementado. Detalhes, configuração do Collector e passo a passo de validação em
> [otel-collector.md](otel-collector.md).

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Até agora temos:

1. OpenTelemetry na API.
2. OpenTelemetry no Worker.
3. OpenTelemetry no Kafka Producer.
4. OpenTelemetry no Kafka Consumer.
5. Propagação de Trace Context através do Kafka.
6. Distributed Tracing funcionando.

Agora introduza o OpenTelemetry Collector.

Explique detalhadamente:

1. O que é OpenTelemetry Collector.
2. Por que usar Collector.
3. O que é OTLP.
4. Receiver.
5. Processor.
6. Exporter.
7. Pipeline.
8. Batch Processor.
9. Resource Attributes.

Crie uma configuração Docker Compose para o Collector.

API e Worker devem enviar telemetria para o Collector.

O Collector deverá receber:

- Traces.
- Metrics, quando aplicável.
- Logs, quando aplicável.

Mantenha a configuração simples e didática.

Explique o fluxo:

API
 -> OTLP
 -> OTel Collector

Worker
 -> OTLP
 -> OTel Collector

Não implemente ainda dashboards finais.

Ao final explique como validar que o Collector está recebendo dados.
```

---

# Fase 14 — Jaeger/Tempo + Prometheus + Grafana

## Objetivo

Adicionar os backends e a visualização da observabilidade.

Arquitetura:

```text
API ───────────────┐
                   │
Worker ────────────┼──► OpenTelemetry Collector
                   │
                   ▼
              ┌───────────┐
              │           │
              ▼           ▼
           Traces      Metrics
              │           │
              ▼           ▼
         Jaeger/Tempo  Prometheus
              │           │
              └─────┬─────┘
                    ▼
                 Grafana
```

### Definition of Done

- [x] Traces disponíveis;
- [x] Metrics disponíveis;
- [x] Grafana configurado;
- [x] Datasources configurados;
- [x] Trace completo visualizável;
- [x] dashboards iniciais.

> Implementado. Detalhes em [observability-backends.md](observability-backends.md).

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Agora adicione os componentes de observabilidade visual.

Tecnologias:

- Jaeger ou Grafana Tempo.
- Prometheus.
- Grafana.

Explique:

1. O que é backend de tracing.
2. O que é backend de métricas.
3. O que é Grafana.
4. O papel do Collector entre aplicações e backends.
5. Como Grafana consulta datasources.

Configure:

OpenTelemetry Collector
 -> Jaeger/Tempo

OpenTelemetry Collector
 -> Prometheus, conforme arquitetura de métricas escolhida

Grafana
 -> Jaeger/Tempo
 -> Prometheus

Crie dashboards para:

- HTTP requests.
- HTTP duration.
- HTTP errors.
- Transaction processing.
- Worker processing.
- Kafka messages.
- Kafka consumer lag, se disponível.
- Processing errors.

Crie também uma visualização de trace distribuído.

O trace deverá permitir investigar:

Blazor
 -> API
 -> PostgreSQL
 -> Kafka Producer
 -> Kafka Consumer
 -> Worker
 -> PostgreSQL

Explique como localizar um TraceId e investigar uma transação problemática.
```

---

# Fase 15 — Logs estruturados

## Objetivo

Adicionar logs estruturados e correlação com observabilidade.

Exemplo:

```json
{
  "Timestamp": "2026-08-01T20:00:00Z",
  "Level": "Information",
  "Message": "Transaction processed",
  "TransactionId": "123",
  "TraceId": "abc123",
  "SpanId": "def456"
}
```

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Implemente logs estruturados utilizando Serilog.

Os logs devem permitir correlação por:

- TransactionId.
- TraceId.
- SpanId.

Explique:

1. Log estruturado.
2. Diferença entre log textual e estruturado.
3. Correlação.
4. Por que TraceId é importante.

Adicione logs relevantes em:

- API.
- Producer.
- Consumer.
- Worker.
- Processamento.
- Erros.

Evite logar dados financeiros sensíveis.

Mostre exemplos de logs esperados.
```

# Fase 14 — Logs estruturados

## Objetivo

Adicionar Serilog e correlação.

Exemplo:

```json
{
  "Timestamp": "2026-08-01T20:00:00Z",
  "Level": "Information",
  "Message": "Transaction processed",
  "TransactionId": "123",
  "TraceId": "abc123",
  "SpanId": "def456"
}
```

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Implemente logs estruturados utilizando Serilog.

Os logs devem permitir correlação por:

- TransactionId.
- TraceId.
- SpanId.

Explique:

1. Log estruturado.
2. Diferença entre log textual e estruturado.
3. Correlação.
4. Por que TraceId é importante.

Adicione logs relevantes em:

- API.
- Producer.
- Consumer.
- Worker.
- Processamento.
- Erros.

Evite logar dados financeiros sensíveis.

Mostre exemplos de logs esperados.
```

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
- [x] OpenTelemetry configurado na API.
- [x] OpenTelemetry configurado no Worker.
- [x] Kafka Producer instrumentado.
- [x] Kafka Consumer instrumentado.
- [x] Trace distribuído funcionando.
- [x] TraceId propagado através do Kafka.
- [x] OpenTelemetry Collector funcionando.
- [x] Prometheus funcionando.
- [x] Grafana funcionando.
- [x] Jaeger/Tempo funcionando.
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
