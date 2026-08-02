# Apache Kafka — conceitos

Este documento explica os conceitos de Kafka usados no projeto FinancialTransaction, introduzidos na infraestrutura da [Fase 6](FASE_06_DOCKER_COMPOSE_DE_INFRAESTRUTURA.md) e utilizados a partir da Fase 7 (Producer) e Fase 8 (Consumer/Worker).

## O que é Kafka

Kafka é uma plataforma de streaming de eventos distribuída. Na prática, funciona como um **log distribuído, particionado e replicável**: produtores (*producers*) anexam mensagens ao final de um log, e consumidores (*consumers*) leem essas mensagens na ordem em que foram escritas, na velocidade que quiserem, cada um mantendo sua própria posição de leitura.

No FinancialTransaction, o Kafka será o meio de comunicação assíncrona entre a API (que cria a transação) e o Worker (que a processa), desacoplando os dois processos no tempo.

## Kafka vs. uma fila tradicional (ex.: RabbitMQ/SQS)

| | Fila tradicional | Kafka |
|---|---|---|
| Mensagem após consumo | Geralmente removida da fila | Permanece no log até expirar (retenção por tempo/tamanho) |
| Múltiplos consumidores independentes | Precisa de uma fila por consumidor (fan-out) | O mesmo tópico pode ser lido por vários Consumer Groups, cada um com sua própria posição |
| Reprocessamento | Difícil ou impossível após o ACK | Possível: basta "rebobinar" o offset e reler mensagens antigas |
| Ordenação | Normalmente não garantida | Garantida dentro de uma partition |
| Modelo | Fila (mensagem sai quando é consumida) | Log (mensagem fica, o consumidor é que avança) |

Isso torna o Kafka mais adequado para *event streaming* (múltiplos consumidores, auditoria, replay) do que para filas de tarefas simples.

## Topic

Um **Topic** é uma categoria/nome de canal onde as mensagens são publicadas — por exemplo, `financial.transactions.created` (usado a partir da Fase 7). Producers publicam em um topic; consumers assinam um topic.

## Partition

Cada Topic é dividido em uma ou mais **Partitions**. Cada partition é um log ordenado e imutável de mensagens. Partitions são a unidade de paralelismo do Kafka: mensagens de partitions diferentes podem ser consumidas em paralelo por instâncias diferentes de um mesmo Consumer Group. Dentro de uma única partition, a ordem de escrita é sempre preservada.

## Offset

O **Offset** é a posição sequencial de uma mensagem dentro de uma partition (0, 1, 2, ...). O Kafka não empurra mensagens para o consumidor de forma destrutiva: o consumidor é que controla e avança seu próprio offset, o que permite reprocessar mensagens antigas ou pausar/retomar o consumo sem perder dados.

## Consumer Group

Um **Consumer Group** é um conjunto de consumidores que cooperam para ler um topic, dividindo as partitions entre si (cada partition é lida por no máximo um consumidor do grupo por vez). Isso permite escalar o consumo horizontalmente: com N partitions, é possível ter até N consumidores processando em paralelo dentro do mesmo grupo. Grupos diferentes são independentes entre si — cada grupo mantém seu próprio conjunto de offsets, então o mesmo topic pode ser consumido de formas diferentes por sistemas diferentes.

## Producer

O **Producer** é quem publica mensagens em um topic. No FinancialTransaction, a `FinancialTransaction.Api` será o producer do evento `TransactionCreated` (Fase 7).

## Consumer

O **Consumer** é quem lê mensagens de um topic, dentro de um Consumer Group. No FinancialTransaction, o `FinancialTransaction.Worker` será o consumer do evento `TransactionCreated` (Fase 8), processando a transação e persistindo o resultado no PostgreSQL.

## KRaft (sem Zookeeper)

Versões mais recentes do Kafka (3.x+) dispensam o Zookeeper: o próprio Kafka assume o papel de coordenação do cluster através do modo **KRaft** (Kafka Raft), onde um ou mais nós atuam como *controller* (metadados do cluster) além de *broker* (armazenamento e servimento de mensagens). A infraestrutura deste projeto usa um único nó combinando os dois papéis (`KAFKA_PROCESS_ROLES: broker,controller`) — adequado para desenvolvimento local, não para produção.
