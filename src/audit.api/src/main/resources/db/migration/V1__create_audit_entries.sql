CREATE TABLE IF NOT EXISTS audit_entries (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id        UUID NOT NULL,
    event_type      VARCHAR(255) NOT NULL,
    aggregate_type  VARCHAR(255) NOT NULL,
    aggregate_id    VARCHAR(255) NOT NULL,
    correlation_id  VARCHAR(255),
    source_service  VARCHAR(255) NOT NULL,
    actor_id        VARCHAR(255),
    actor_type      VARCHAR(50)  NOT NULL,
    old_value       JSONB,
    new_value       JSONB,
    metadata        JSONB,
    occurred_at     TIMESTAMP WITH TIME ZONE NOT NULL,
    recorded_at     TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    version         BIGINT NOT NULL DEFAULT 0,
    hash            VARCHAR(64) NOT NULL,
    previous_hash   VARCHAR(64),
    CONSTRAINT uq_event_id UNIQUE (event_id)
);

CREATE INDEX IF NOT EXISTS idx_audit_aggregate_id   ON audit_entries (aggregate_id);
CREATE INDEX IF NOT EXISTS idx_audit_correlation_id ON audit_entries (correlation_id);
CREATE INDEX IF NOT EXISTS idx_audit_occurred_at    ON audit_entries (occurred_at);
CREATE INDEX IF NOT EXISTS idx_audit_event_type     ON audit_entries (event_type);
