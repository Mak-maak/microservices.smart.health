# SmartHealth Payments API (Go)

A production-ready payments microservice written in Go for the SmartHealth platform.

## Architecture

- **Clean Architecture** with **Vertical Slice** (feature-based) organization
- **CQRS** pattern via a lightweight Go mediator (type-switch dispatch)
- **Transactional Outbox Pattern** for reliable event publishing
- **RabbitMQ** for messaging (in-memory no-op for dev/testing)
- **PostgreSQL** with `pgx/v5`
- **Stripe Go SDK** (test mode) for payment processing

## Business Flow

```
AppointmentSlotReserved (RabbitMQ) → CreatePayment (Pending)
  → Stripe CreatePaymentIntent
  → MarkCompleted / MarkFailed
  → PaymentCompletedIntegrationEvent → Outbox → RabbitMQ
```

## Folder Structure

```
src/payments.api/
├── cmd/api/main.go              # Entry point, DI, bootstrap, graceful shutdown
├── internal/
│   ├── payments/
│   │   ├── domain/              # Payment aggregate, events, errors
│   │   ├── create_payment/      # CQRS command + handler (with idempotency)
│   │   ├── complete_payment/    # CQRS command + handler (Stripe integration)
│   │   ├── get_payment/         # CQRS query + handler
│   │   └── infrastructure/      # PostgreSQL repository
│   ├── outbox/                  # Outbox message, repository, background worker
│   ├── messaging/               # RabbitMQ consumer/publisher + event contracts
│   ├── database/                # PostgreSQL connection pool
│   ├── stripe/                  # Stripe service interface + implementation
│   └── shared/                  # Lightweight mediator + config
├── migrations/                  # SQL migration files
├── go.mod
└── Dockerfile
```

## Event Contracts

**Incoming (consumed from RabbitMQ):**
```json
{ "appointmentId": "uuid", "userId": "string", "amount": 100.00, "currency": "usd" }
```

**Outgoing (published to RabbitMQ via Outbox):**
```json
{ "paymentId": "uuid", "appointmentId": "uuid", "status": "Completed", "transactionId": "pi_stripe_id" }
```

## Configuration

| Variable | Default | Description |
|---|---|---|
| `PORT` | `8080` | HTTP server port |
| `ENVIRONMENT` | `development` | Environment (production uses JSON logging) |
| `DATABASE_URL` | `postgres://...localhost...` | PostgreSQL DSN |
| `RABBITMQ_URL` | `amqp://guest:guest@localhost:5672/` | RabbitMQ URL |
| `USE_IN_MEMORY_BROKER` | `true` | Skip RabbitMQ (for dev/testing) |
| `STRIPE_SECRET_KEY` | `sk_test_placeholder` | Stripe API key |

## Running Locally

```bash
cd src/payments.api
go run ./cmd/api
```

Or with Docker:
```bash
docker-compose up payments-api
```

## Health Endpoints

- `GET /health` – liveness
- `GET /readiness` – checks DB connectivity
- `GET /liveness` – alias

## API Endpoints

- `GET /api/payments/:id` – get payment details
- `POST /api/payments/trigger` – manually trigger a payment (dev only)

## Running Tests

```bash
cd src/payments.api
go test ./...
```
