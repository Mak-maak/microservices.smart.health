# Event Recorder API

A NestJS microservice that records domain events from multiple services in the Smart Health platform and provides read-only query endpoints.

## Architecture

### Overview

```
Azure Service Bus Topics
  ├── appointment-created  ─┐
  ├── payment-completed    ─┤
  ├── payment-failed       ─┼─► DomainEventConsumer ─► RecordEventCommand ─► CosmosDB
  ├── prescription-created ─┤
  └── shipment-dispatched  ─┘

REST Endpoints (read-only)
  GET /api/events/aggregate/:aggregateId
  GET /api/events/correlation/:correlationId
  GET /api/events/type/:eventType
  GET /api/events/date-range?from=&to=
  GET /health
```

### CQRS Pattern

The service applies Command Query Responsibility Segregation:

- **Commands** (`RecordEventCommand`) are dispatched when a domain event message arrives from Service Bus. The `RecordEventHandler` builds an `EventRecordDocument` and persists it to Cosmos DB.
- **Queries** (`GetEventsByAggregateQuery`, `GetEventsByCorrelationQuery`, `GetEventsByTypeQuery`, `GetEventsByDateRangeQuery`) are dispatched from REST controllers. Each has a dedicated handler that delegates to the `EventRecordRepository`.

This separation keeps write and read paths independent and easy to scale or replace.

### Partition Key Choice

The Cosmos DB container uses `/aggregateId` as the partition key. Domain events naturally cluster around a single aggregate (e.g., all events for Appointment `abc-123`). Queries by `aggregateId` are therefore single-partition and extremely fast. Cross-partition queries (by correlation ID, event type, or date range) are supported via fan-out and are acceptable for audit / analytics workloads.

### Event Flow

1. A domain event is published to an Azure Service Bus **topic** by a source service (appointments, payments, prescriptions, shipments).
2. `DomainEventConsumer` subscribes to each topic using a shared **subscription name** (`eventrecorder-service`).
3. On receipt, the consumer maps the message to a `RecordEventCommand` and dispatches it via the `CommandBus`.
4. `RecordEventHandler` builds an `EventRecordDocument` with a new `id` (UUID), `recordedAt` timestamp, and all event metadata.
5. `EventRecordRepository.save()` performs an **idempotency check** before writing.
6. On success the receiver calls `completeMessage()` to remove the message from the subscription.

### Idempotency Strategy

Before persisting, `EventRecordRepository.save()` queries Cosmos DB for an existing document with the same `eventId`. If one already exists it is returned immediately without a second write. This means:

- Re-delivered Service Bus messages are silently deduplicated.
- The `eventId` field is the canonical idempotency key; publishers must populate it (UUID per event).
- Cosmos DB's `id` field is a separate surrogate key (generated fresh each time, but only used if no duplicate is found).

### Scaling Strategy

| Concern | Approach |
|---|---|
| **Throughput** | Add more Service Bus subscription sessions / increase `maxConcurrentCalls` on each receiver. |
| **Storage** | Cosmos DB autoscale RU/s; add more partitions if hot spots appear. |
| **Read latency** | `aggregateId` queries are single-partition. For other query patterns consider adding Cosmos DB composite indexes. |
| **Horizontal scale** | Stateless NestJS process – deploy multiple replicas behind a load balancer. Each replica independently consumes from Service Bus (competing consumers pattern). |
| **Observability** | Every operation logs with correlation ID. Extend with Azure Monitor / Application Insights via the `@azure/monitor-opentelemetry` package. |

## Configuration

Copy `.env.example` to `.env` and fill in the values:

| Variable | Description |
|---|---|
| `PORT` | HTTP port (default `3003`) |
| `COSMOS_DB_ENDPOINT` | Cosmos DB account endpoint |
| `COSMOS_DB_KEY` | Cosmos DB primary key |
| `COSMOS_DB_DATABASE` | Database name |
| `COSMOS_DB_CONTAINER` | Container name |
| `AZURE_SERVICE_BUS_CONNECTION_STRING` | Service Bus namespace connection string |
| `TOPIC_*` | Topic names for each event type |
| `SUBSCRIPTION_NAME` | Shared subscription name |

## Running Locally

```bash
npm install
npm run start:dev
```

## Running Tests

```bash
npm test
npm run test:cov
```

## Building

```bash
npm run build
npm run start:prod
```

## REST Endpoints

| Method | Path | Description |
|---|---|---|
| `GET` | `/health` | Health check |
| `GET` | `/api/events/aggregate/:aggregateId` | All events for an aggregate |
| `GET` | `/api/events/correlation/:correlationId` | All events sharing a correlation ID |
| `GET` | `/api/events/type/:eventType` | All events of a given type |
| `GET` | `/api/events/date-range?from=&to=` | Events within an ISO 8601 date range |
