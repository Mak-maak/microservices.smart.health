-- Payments table
CREATE TABLE IF NOT EXISTS payments (
    id                      UUID        PRIMARY KEY,
    appointment_id          UUID        NOT NULL UNIQUE,   -- unique enforces idempotency
    user_id                 VARCHAR(256) NOT NULL,
    amount                  NUMERIC(18,2) NOT NULL,
    currency                VARCHAR(3)  NOT NULL,
    status                  INT         NOT NULL DEFAULT 0,
    stripe_payment_intent_id VARCHAR(255),
    failure_reason          VARCHAR(1000),
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_payments_appointment_id ON payments(appointment_id);
CREATE INDEX IF NOT EXISTS idx_payments_status ON payments(status);

-- Outbox messages table (Transactional Outbox Pattern)
CREATE TABLE IF NOT EXISTS outbox_messages (
    id           UUID        PRIMARY KEY,
    aggregate_id UUID        NOT NULL,
    type         VARCHAR(255) NOT NULL,
    payload      JSONB       NOT NULL,
    processed    BOOLEAN     NOT NULL DEFAULT false,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    processed_at TIMESTAMPTZ,
    retry_count  INT         NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS idx_outbox_unprocessed ON outbox_messages(processed, retry_count, created_at)
    WHERE processed = false;
