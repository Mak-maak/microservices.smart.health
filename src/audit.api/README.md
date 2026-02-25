# Audit API

Production-grade auditing microservice for the Smart Health platform, built with **Java 17** and **Spring Boot 3**.

## Table of Contents

1. [Architecture Decisions](#architecture-decisions)
2. [Hash Chaining](#hash-chaining)
3. [Running Locally](#running-locally)
4. [Event Contracts](#event-contracts)
5. [API Endpoints](#api-endpoints)
6. [Event Payload Examples](#event-payload-examples)

---

## Architecture Decisions

### Java 17 / Spring Boot 3

- **Java 17 LTS** provides records, sealed classes, pattern matching, and virtual threads (Project Loom preview).
- **Spring Boot 3.2** brings native AOT compilation support, improved observability, and full Jakarta EE 10 compatibility.

### Vertical Slice Architecture

Each feature lives in its own package (`features/<feature-name>/`), containing its query/command, handler, and controller. This co-locates all code related to a feature and eliminates cross-cutting changes when requirements evolve.

```
features/
├── storeaudit/          ← write side
├── getauditbyaggregate/ ← read side
├── getauditbycorrelation/
├── getauditbydaterange/
└── getauditbyeventtype/
```

### CQRS

Commands and queries are separated at the handler level:
- **Commands** (`StoreAuditEntryCommand`) mutate state and are dispatched by the messaging layer.
- **Queries** (`GetAuditBy*Query`) are read-only and dispatched by REST controllers.

Interfaces live in `shared/`:

```java
Command<R>, CommandHandler<C, R>
Query<R>,   QueryHandler<Q, R>
```

### Mediator (Simplified)

Rather than a full-blown mediator library (MediatR-style), each controller/listener directly injects its own handler. This keeps the DI graph explicit, avoids reflection overhead, and makes handler wiring visible at compile time.

### Immutable Audit Entries

`AuditEntry` is an **append-only**, write-once entity:
- No setters — only an all-args constructor.
- JPA requires a protected no-args constructor (not public).
- `@PrePersist` sets `recordedAt` automatically.

### Hash Chaining

Every audit entry contains a `hash` and `previousHash`, forming a linked chain that makes tampering detectable.

---

## Hash Chaining

Each new audit entry's hash is computed as:

```
SHA-256(previousHash | eventId | eventType | aggregateId | occurredAt)
```

The genesis entry uses `"GENESIS"` as `previousHash`.

### Tamper Detection

Because each entry includes the hash of the previous entry, any modification to a historical record breaks the chain from that point forward. A chain-validation sweep can detect:

- Modified field values (the hash won't match its inputs).
- Deleted entries (gap in the `previousHash` chain).
- Inserted entries (collision in the `previousHash` chain).

---

## Running Locally

### Prerequisites

- Docker & Docker Compose
- Java 17 (only if building outside Docker)

### With Docker Compose

Add the following service to `docker-compose.yml` at the repository root:

```yaml
audit-api:
  build:
    context: ./src/audit.api
  ports:
    - "8082:8082"
  environment:
    DATABASE_URL: jdbc:postgresql://postgres:5432/smarthealth_audit
    DATABASE_USERNAME: postgres
    DATABASE_PASSWORD: postgres
    JWT_SECRET: change-me-in-production-use-32-chars
    AZURE_SERVICE_BUS_CONNECTION_STRING: ""   # leave empty to skip messaging
  depends_on:
    - postgres

postgres:
  image: postgres:16-alpine
  environment:
    POSTGRES_DB: smarthealth_audit
    POSTGRES_USER: postgres
    POSTGRES_PASSWORD: postgres
  ports:
    - "5432:5432"
```

```bash
docker compose up audit-api
```

### Building Manually

```bash
cd src/audit.api
mvn package -DskipTests
java -jar target/audit-api-1.0.0.jar \
  --spring.datasource.url=jdbc:postgresql://localhost:5432/smarthealth_audit \
  --jwt.secret=change-me
```

### OpenAPI / Swagger UI

Navigate to `http://localhost:8082/swagger-ui.html` once the service is running.

---

## Event Contracts

### From `appointments.api` (Azure Service Bus — topic: `appointments-events`)

| Field         | Type   | Description                                          |
|---------------|--------|------------------------------------------------------|
| eventId       | string | UUID of the integration event                        |
| eventType     | string | `AppointmentRequested`, `AppointmentConfirmed`, `AppointmentCancelled` |
| appointmentId | string | Appointment aggregate ID                             |
| patientId     | string | Patient ID                                           |
| doctorId      | string | Doctor ID                                            |
| reason        | string | Cancellation reason (only for `AppointmentCancelled`)|
| startTime     | string | ISO-8601 datetime                                    |
| endTime       | string | ISO-8601 datetime                                    |
| occurredAt    | string | ISO-8601 datetime                                    |

### From `payments.api` (Azure Service Bus — topic: `payments-events`)

| Field         | Type   | Description                                          |
|---------------|--------|------------------------------------------------------|
| eventId       | string | UUID of the integration event                        |
| eventType     | string | `PaymentCompleted`, `PaymentFailed`, `PaymentInitiated` |
| paymentId     | string | Payment aggregate ID                                 |
| appointmentId | string | Related appointment ID                               |
| status        | string | Payment status                                       |
| transactionId | string | External transaction reference                       |
| reason        | string | Failure reason (only for `PaymentFailed`)            |
| occurredAt    | string | ISO-8601 datetime                                    |

---

## API Endpoints

All endpoints require a Bearer JWT token with the `AUDIT_READ` role.

| Method | Path                                   | Description                            |
|--------|----------------------------------------|----------------------------------------|
| GET    | `/api/audit/{aggregateId}`             | Get audit entries by aggregate ID      |
| GET    | `/api/audit/{aggregateId}/history`     | Alias for the above                    |
| GET    | `/api/audit/correlation/{correlationId}` | Get entries by correlation ID        |
| GET    | `/api/audit?from=&to=`                 | Get entries in a date range            |
| GET    | `/api/audit?eventType=`                | Get entries by event type              |
| GET    | `/api/audit/events?eventType=`         | Dedicated event-type endpoint          |
| GET    | `/actuator/health`                     | Health check (public)                  |
| GET    | `/actuator/prometheus`                 | Prometheus metrics (public)            |

All list endpoints support pagination via `page`, `size`, and `sort` query parameters.

### Example curl Commands

```bash
# Get JWT token (example — adapt to your auth service)
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# Get audit trail for an appointment
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:8082/api/audit/appointment-123?page=0&size=10&sort=occurredAt,desc"

# Get audit history (alias)
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:8082/api/audit/appointment-123/history"

# Get by correlation ID
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:8082/api/audit/correlation/corr-456"

# Get by date range
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:8082/api/audit?from=2024-01-01T00:00:00Z&to=2024-12-31T23:59:59Z"

# Get by event type
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:8082/api/audit?eventType=AppointmentRequested"
```

---

## Event Payload Examples

### AppointmentRequestedIntegrationEvent

```json
{
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "eventType": "AppointmentRequested",
  "appointmentId": "appt-001",
  "patientId": "patient-42",
  "doctorId": "doctor-7",
  "startTime": "2024-06-15T09:00:00Z",
  "endTime": "2024-06-15T09:30:00Z",
  "occurredAt": "2024-06-14T15:32:00Z"
}
```

### AppointmentCancelledIntegrationEvent

```json
{
  "eventId": "660e8400-e29b-41d4-a716-446655440001",
  "eventType": "AppointmentCancelled",
  "appointmentId": "appt-001",
  "reason": "Patient request",
  "occurredAt": "2024-06-14T16:00:00Z"
}
```

### PaymentCompletedIntegrationEvent

```json
{
  "eventId": "770e8400-e29b-41d4-a716-446655440002",
  "eventType": "PaymentCompleted",
  "paymentId": "pay-999",
  "appointmentId": "appt-001",
  "status": "COMPLETED",
  "transactionId": "txn-abc123",
  "occurredAt": "2024-06-14T15:45:00Z"
}
```

### PaymentFailedIntegrationEvent

```json
{
  "eventId": "880e8400-e29b-41d4-a716-446655440003",
  "eventType": "PaymentFailed",
  "paymentId": "pay-1000",
  "appointmentId": "appt-002",
  "reason": "Insufficient funds",
  "occurredAt": "2024-06-14T15:50:00Z"
}
```

---

## Security

- JWT validation uses HMAC-256 with a configurable secret (`JWT_SECRET` env var).
- Roles are extracted from the `roles` claim in the JWT payload.
- Actuator health and Prometheus metrics are public.
- All `/api/audit/**` routes require authentication + `AUDIT_READ` role.
- Session management is stateless (no HTTP sessions).
- CSRF protection is disabled (stateless API).
