# Prescriptions API

A production-grade Python FastAPI microservice for managing medical prescriptions, with ML-based training and LLM-powered suggestion capabilities.

## Architecture

The service follows **Vertical Slice Architecture** with a **CQRS** pattern (Commands/Queries), a lightweight **Mediator**, and an **event-driven** messaging layer.

```
app/
├── config/         – Pydantic settings loaded from environment / .env
├── domain/         – SQLAlchemy ORM entities + domain events (Pydantic)
├── infrastructure/ – Async DB engine, session factory, repositories
├── messaging/      – Azure Service Bus publisher & subscriber (in-memory fallback)
├── mediator/       – Simple async mediator for dispatching commands/queries
├── security/       – JWT bearer token verification (python-jose)
└── features/
    ├── create_prescription/   – POST /api/prescriptions
    ├── get_prescription/      – GET  /api/prescriptions/{id} | /patient/{id}
    ├── suggest_prescription/  – POST /api/prescriptions/suggest  (LLM)
    └── train_model/           – POST /api/prescriptions/train    (scikit-learn)
```

### Key technology choices

| Concern | Library |
|---------|---------|
| Web framework | FastAPI 0.115 |
| ORM / async DB | SQLAlchemy 2 asyncio + asyncpg |
| Migrations | Alembic |
| Messaging | Azure Service Bus (in-memory fallback via `USE_IN_MEMORY_BUS=true`) |
| ML training | scikit-learn TF-IDF + Logistic Regression, persisted with joblib |
| LLM suggestions | OpenAI / Azure OpenAI (`gpt-4o-mini`) |
| Settings | pydantic-settings |
| Structured logging | structlog |
| Auth | python-jose JWT |

---

## Quick start

### 1. Prerequisites

- Python 3.11+
- PostgreSQL (or Docker)
- (Optional) Azure Service Bus namespace
- (Optional) OpenAI API key

### 2. Install dependencies

```bash
cd src/prescriptions.api
python -m venv .venv
source .venv/bin/activate      # Windows: .venv\Scripts\activate
pip install -r requirements.txt
```

### 3. Configure environment

```bash
cp .env.example .env
# Edit .env – at minimum set DATABASE_URL
```

### 4. Run database migrations

```bash
alembic upgrade head
```

### 5. Start the service

```bash
uvicorn app.main:app --reload --port 8000
```

Interactive docs available at <http://localhost:8000/docs>.

---

## Running with Docker

```bash
docker build -t prescriptions-api .
docker run -p 8000:8000 \
  -e DATABASE_URL="postgresql+asyncpg://postgres:postgres@host.docker.internal:5432/smarthealth_prescriptions" \
  prescriptions-api
```

---

## API Endpoints

### Health

```
GET /health
```

```json
{
  "status": "healthy",
  "service": "Prescriptions API",
  "version": "1.0.0",
  "environment": "production"
}
```

---

### Create prescription

```
POST /api/prescriptions
Content-Type: application/json
X-Correlation-Id: <optional uuid>
```

**Request body**

```json
{
  "appointment_id": "550e8400-e29b-41d4-a716-446655440000",
  "patient_id":     "550e8400-e29b-41d4-a716-446655440001",
  "doctor_id":      "550e8400-e29b-41d4-a716-446655440002",
  "symptoms":       ["fever", "headache", "fatigue"],
  "diagnosis":      "Influenza A",
  "medications":    ["Oseltamivir", "Paracetamol"],
  "dosage":         {"Oseltamivir": "75mg twice daily", "Paracetamol": "500mg every 6h"},
  "notes":          "Rest and increase fluid intake"
}
```

**cURL**

```bash
curl -s -X POST http://localhost:8000/api/prescriptions \
  -H "Content-Type: application/json" \
  -d '{
    "appointment_id": "550e8400-e29b-41d4-a716-446655440000",
    "patient_id":     "550e8400-e29b-41d4-a716-446655440001",
    "doctor_id":      "550e8400-e29b-41d4-a716-446655440002",
    "symptoms":       ["fever","headache"],
    "diagnosis":      "Influenza A",
    "medications":    ["Oseltamivir"],
    "dosage":         {"Oseltamivir":"75mg twice daily"},
    "notes":          null
  }' | jq
```

---

### Get prescription by ID

```
GET /api/prescriptions/{prescription_id}
```

```bash
curl -s http://localhost:8000/api/prescriptions/550e8400-e29b-41d4-a716-446655440010 | jq
```

---

### Get prescriptions by patient

```
GET /api/prescriptions/patient/{patient_id}
```

```bash
curl -s http://localhost:8000/api/prescriptions/patient/550e8400-e29b-41d4-a716-446655440001 | jq
```

---

### Suggest prescription (LLM)

```
POST /api/prescriptions/suggest
Content-Type: application/json
```

**Request body**

```json
{
  "symptoms": ["persistent cough", "shortness of breath", "low-grade fever"],
  "patient_history": "Smoker, 45 years old, no known allergies"
}
```

**cURL**

```bash
curl -s -X POST http://localhost:8000/api/prescriptions/suggest \
  -H "Content-Type: application/json" \
  -d '{"symptoms":["cough","fever"],"patient_history":null}' | jq
```

> Requires `OPENAI_API_KEY` to be set and `ENABLE_LLM_SUGGESTIONS=true`.  
> Returns a fallback response when the LLM is unavailable.

---

### Train ML model

```
POST /api/prescriptions/train
Content-Type: application/json
```

```json
{ "force_retrain": false }
```

```bash
curl -s -X POST http://localhost:8000/api/prescriptions/train \
  -H "Content-Type: application/json" \
  -d '{"force_retrain": true}' | jq
```

Trains a TF-IDF + Logistic Regression model on all stored prescriptions and saves it to `MODEL_STORAGE_PATH/prescription_model.pkl`.

---

## Event payloads

### PrescriptionSavedEvent (topic: `prescription-saved`)

```json
{
  "event_id":       "uuid",
  "event_type":     "PrescriptionSavedEvent",
  "correlation_id": "uuid",
  "aggregate_id":   "prescription-uuid",
  "occurred_at":    "2024-01-01T00:00:00+00:00",
  "source_service": "prescriptions.api",
  "payload": {
    "prescription_id": "uuid",
    "patient_id":      "uuid",
    "doctor_id":       "uuid",
    "appointment_id":  "uuid",
    "diagnosis":       "Influenza A"
  }
}
```

### PrescriptionSuggestedEvent (topic: `prescription-suggested`)

```json
{
  "event_type": "PrescriptionSuggestedEvent",
  "payload": {
    "symptoms":   ["fever", "headache"],
    "diagnosis":  "Viral infection",
    "confidence": 0.82
  }
}
```

---

## Environment variables reference

| Variable | Default | Description |
|----------|---------|-------------|
| `DATABASE_URL` | `postgresql+asyncpg://...` | Async PostgreSQL connection string |
| `USE_IN_MEMORY_BUS` | `true` | Skip Azure Service Bus (log-only) |
| `AZURE_SERVICE_BUS_CONNECTION_STRING` | _(empty)_ | Required when `USE_IN_MEMORY_BUS=false` |
| `OPENAI_API_KEY` | _(empty)_ | OpenAI key for LLM suggestions |
| `ENABLE_LLM_SUGGESTIONS` | `true` | Toggle LLM feature |
| `USE_AZURE_OPENAI` | `false` | Use Azure OpenAI instead of OpenAI |
| `SECRET_KEY` | `change-me-in-production` | JWT signing key |
| `MODEL_STORAGE_PATH` | `./models` | Directory for persisted ML models |
| `DEBUG` | `false` | Enable SQLAlchemy echo + verbose logging |

See `.env.example` for the full list.

---

## Project structure

```
src/prescriptions.api/
├── app/
│   ├── main.py                        ← FastAPI application entry-point
│   ├── config/settings.py             ← Pydantic-settings configuration
│   ├── domain/
│   │   ├── entities.py                ← SQLAlchemy ORM models
│   │   └── events.py                  ← Domain event Pydantic models
│   ├── infrastructure/
│   │   ├── database.py                ← Async engine + session factory
│   │   └── repositories.py            ← Data-access layer
│   ├── messaging/
│   │   ├── publisher.py               ← Azure Service Bus publisher
│   │   └── subscriber.py              ← Azure Service Bus subscriber
│   ├── mediator/mediator.py           ← Lightweight async mediator
│   ├── security/auth.py               ← JWT verification helper
│   └── features/
│       ├── create_prescription/       ← Command + Handler + Router + Schemas
│       ├── get_prescription/          ← Query  + Handler + Router + Schemas
│       ├── suggest_prescription/      ← Query  + Handler + Router + Schemas (LLM)
│       └── train_model/               ← Command + Handler + Router + Schemas (ML)
├── migrations/
│   ├── env.py
│   ├── script.py.mako
│   └── versions/001_initial.py
├── alembic.ini
├── Dockerfile
├── requirements.txt
└── .env.example
```
