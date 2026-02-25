# SmartHealth Payments API

A production-ready payments microservice for the SmartHealth platform.

## Architecture

This service follows the same architectural conventions as `SmartHealth.Appointments.API`:

- **Clean Architecture** with **Vertical Slice** (feature-based) organization
- **CQRS** via MediatR (Commands and Queries)
- **Transactional Outbox Pattern** for reliable event publishing
- **MassTransit** for messaging (Azure Service Bus in production, in-memory for dev)
- **Entity Framework Core** with SQL Server
- **Stripe.net SDK** (test mode) for payment processing

## Business Flow

```
AppointmentSlotReserved (consumed)
  → CreatePayment (Pending)
  → CreatePaymentIntent (Stripe)
  → MarkCompleted / MarkFailed
  → PaymentCompletedIntegrationEvent → Outbox → Published
```

## Event Contracts

**Incoming:**
```json
{
  "appointmentId": "guid",
  "userId": "string",
  "amount": 100.00,
  "currency": "usd"
}
```

**Outgoing:**
```json
{
  "paymentId": "guid",
  "appointmentId": "guid",
  "status": "Completed",
  "transactionId": "pi_stripe_id"
}
```

## Configuration

| Key | Description |
|-----|-------------|
| `ConnectionStrings:SqlServer` | SQL Server connection string |
| `ConnectionStrings:AzureServiceBus` | Azure Service Bus connection string |
| `Stripe:SecretKey` | Stripe API secret key (use `sk_test_*` for test mode) |
| `Features:UseInMemoryBus` | Use in-memory bus (for local dev/testing) |

## Running Locally

```bash
docker-compose up
```

## Health Endpoints

- `GET /health` – Overall health
- `GET /readiness` – Readiness check (DB + Service Bus)
- `GET /liveness` – Liveness check
