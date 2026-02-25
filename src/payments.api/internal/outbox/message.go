package outbox

import (
	"time"

	"github.com/google/uuid"
)

// Message represents an outbox entry for reliable event publishing.
//
// Architectural Decision: The Transactional Outbox Pattern ensures
// that domain state changes and the corresponding outbox entries are
// persisted atomically. A background worker then reads unpublished
// entries and publishes them to the message broker, guaranteeing
// at-least-once delivery without the dual-write problem.
type Message struct {
	ID          uuid.UUID
	AggregateID uuid.UUID
	Type        string     // event type name (e.g. "PaymentCompletedIntegrationEvent")
	Payload     []byte     // JSON-encoded event payload
	Processed   bool
	CreatedAt   time.Time
	ProcessedAt *time.Time
	RetryCount  int
}
