# Shipment Microservice

A production-grade NestJS microservice for managing medication shipments in the Smart Health platform.

## Overview

The Shipment Microservice handles the complete lifecycle of medication shipments—from creation triggered by prescription events, through packing and dispatch, to final delivery. It integrates with Azure Service Bus for event-driven communication and uses PostgreSQL via Prisma ORM for persistence.

## Architecture

### Design Patterns

- **CQRS (Command Query Responsibility Segregation)**: Commands mutate state; queries read state. Powered by `@nestjs/cqrs`.
- **Domain-Driven Design**: Rich domain model with encapsulated business rules.
- **Event-Driven Architecture**: Publishes domain events to Azure Service Bus topics.
- **Idempotent Message Processing**: Deduplicates incoming messages via `ProcessedMessage` table.

### Domain Model

```
Shipment
├── id: UUID
├── prescriptionId: UUID
├── patientId: UUID
├── pharmacyId: UUID
├── medications: MedicationItem[]
├── address: Address
├── shipmentStatus: ShipmentStatus
├── trackingNumber?: string
└── version: int (optimistic concurrency)
```

### State Machine

```
CREATED → PACKED → DISPATCHED → DELIVERED
    ↓         ↓          ↓
  FAILED    FAILED     FAILED
```

Valid transitions:
| From       | To                        |
|------------|---------------------------|
| CREATED    | PACKED, FAILED            |
| PACKED     | DISPATCHED, FAILED        |
| DISPATCHED | DELIVERED, FAILED         |
| DELIVERED  | (terminal)                |
| FAILED     | (terminal)                |

### Project Structure

```
src/
├── app.module.ts                    # Root module
├── main.ts                          # Application bootstrap
├── config/
│   └── configuration.ts             # Environment configuration
├── domain/
│   ├── shipment.entity.ts           # Domain entity + state machine
│   ├── shipment-status.enum.ts      # Status enum
│   └── events/                      # Domain event types
├── infrastructure/
│   ├── database/
│   │   ├── prisma.service.ts        # Prisma client wrapper
│   │   └── shipment.repository.ts   # Data access layer
│   └── messaging/
│       ├── service-bus.service.ts   # Azure Service Bus client
│       └── service-bus.module.ts    # Global messaging module
├── messaging/
│   ├── consumers/                   # Inbound message handlers
│   │   ├── prescription-created.consumer.ts
│   │   └── medicines-prescribed.consumer.ts
│   └── publishers/
│       └── shipment-event.publisher.ts  # Outbound event publisher
├── features/
│   ├── create-shipment/             # Create shipment feature slice
│   ├── dispatch-shipment/           # Dispatch shipment feature slice
│   ├── mark-delivered/              # Mark delivered feature slice
│   ├── fail-shipment/               # Fail shipment feature slice
│   └── get-shipment/                # Query feature slice
└── common/
    ├── correlation-id.middleware.ts # Request correlation ID
    └── logger.service.ts            # Application logger
```

## API Endpoints

### Commands (Write)

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/shipments/dispatch` | Dispatch a shipment with tracking number |
| `POST` | `/api/shipments/deliver` | Mark shipment as delivered |

### Queries (Read)

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/shipments/:id` | Get shipment by ID |
| `GET` | `/api/shipments/prescription/:prescriptionId` | Get shipments by prescription |
| `GET` | `/api/shipments/patient/:patientId` | Get shipments by patient |

### Health Check

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/health` | Service health status |

### Example: Dispatch Shipment

```http
POST /api/shipments/dispatch
Content-Type: application/json
x-correlation-id: 550e8400-e29b-41d4-a716-446655440000

{
  "shipmentId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "trackingNumber": "1Z999AA10123456784"
}
```

### Example: Mark Delivered

```http
POST /api/shipments/deliver
Content-Type: application/json

{
  "shipmentId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

## Event-Driven Integration

### Subscribed Topics (Inbound)

| Topic | Subscription | Description |
|-------|-------------|-------------|
| `prescription-created` | `shipment-service` | Triggers shipment creation |
| `medicines-prescribed` | `shipment-service` | Triggers shipment creation |

### Published Topics (Outbound)

| Topic | Event Type | Description |
|-------|-----------|-------------|
| `shipment-created` | `ShipmentCreated` | Shipment successfully created |
| `shipment-dispatched` | `ShipmentDispatched` | Shipment dispatched with tracking |
| `shipment-delivered` | `ShipmentDelivered` | Shipment delivered |
| `shipment-failed` | `ShipmentFailed` | Shipment processing failed |
| `audit-events` | `AuditEvent` | All state transitions for audit trail |

### Message Format

```json
{
  "messageId": "uuid",
  "correlationId": "uuid",
  "subject": "ShipmentCreated",
  "body": { ... },
  "applicationProperties": {
    "eventType": "ShipmentCreated",
    "sourceService": "shipment-service",
    "occurredAt": "2024-01-01T00:00:00.000Z"
  }
}
```

## Environment Variables

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `PORT` | No | `3000` | HTTP server port |
| `NODE_ENV` | No | `development` | Environment name |
| `DATABASE_URL` | **Yes** | — | PostgreSQL connection string |
| `AZURE_SERVICE_BUS_CONNECTION_STRING` | No | — | Azure Service Bus connection string |
| `TOPIC_PRESCRIPTION_CREATED` | No | `prescription-created` | Inbound topic name |
| `TOPIC_MEDICINES_PRESCRIBED` | No | `medicines-prescribed` | Inbound topic name |
| `SUBSCRIPTION_NAME` | No | `shipment-service` | Service Bus subscription name |
| `TOPIC_SHIPMENT_CREATED` | No | `shipment-created` | Outbound topic name |
| `TOPIC_SHIPMENT_DISPATCHED` | No | `shipment-dispatched` | Outbound topic name |
| `TOPIC_SHIPMENT_DELIVERED` | No | `shipment-delivered` | Outbound topic name |
| `TOPIC_SHIPMENT_FAILED` | No | `shipment-failed` | Outbound topic name |
| `TOPIC_AUDIT_EVENTS` | No | `audit-events` | Audit events topic name |

## Local Development

### Prerequisites

- Node.js 20+
- PostgreSQL 15+
- npm

### Setup

```bash
# Install dependencies
npm install

# Copy environment file
cp .env.example .env
# Edit .env with your database URL

# Generate Prisma client
npm run prisma:generate

# Run database migrations (development)
npm run prisma:migrate:dev

# Start development server with hot reload
npm run start:dev
```

### Build

```bash
npm run build
```

### Tests

```bash
# Unit tests
npm test

# Test coverage
npm run test:cov
```

## Docker

### Build Image

```bash
docker build -t shipment-api:latest .
```

### Run Container

```bash
docker run -p 3000:3000 \
  -e DATABASE_URL=postgresql://postgres:postgres@host.docker.internal:5432/smarthealth_shipment \
  -e AZURE_SERVICE_BUS_CONNECTION_STRING="your-connection-string" \
  shipment-api:latest
```

### Docker Compose

See the root `docker-compose.yml` for full platform orchestration including this service.

## Database Schema

### Shipment Table

| Column | Type | Description |
|--------|------|-------------|
| `id` | TEXT (UUID) | Primary key |
| `prescriptionId` | TEXT | Reference to prescription |
| `patientId` | TEXT | Reference to patient |
| `pharmacyId` | TEXT | Reference to pharmacy |
| `medications` | JSONB | Array of medication items |
| `shipmentStatus` | TEXT | Current status |
| `trackingNumber` | TEXT? | Carrier tracking number |
| `address` | JSONB | Delivery address |
| `createdAt` | TIMESTAMP | Creation time |
| `updatedAt` | TIMESTAMP | Last update time |
| `version` | INTEGER | Optimistic concurrency version |

### ProcessedMessage Table

Used for idempotent message processing. Stores message IDs of already-processed Service Bus messages.

| Column | Type | Description |
|--------|------|-------------|
| `id` | TEXT | Message ID (primary key) |
| `shipmentId` | TEXT? | Associated shipment |
| `processedAt` | TIMESTAMP | Processing time |
