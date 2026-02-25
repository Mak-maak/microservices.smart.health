package outbox

import (
	"context"
	"encoding/json"
	"fmt"
	"time"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"
	"github.com/smart-health/payments-api/internal/messaging"
	"github.com/smart-health/payments-api/internal/payments/domain"
)

// Repository defines the outbox persistence contract.
type Repository interface {
	// SaveEvents translates domain events to outbox messages within an existing transaction.
	SaveEvents(ctx context.Context, tx pgx.Tx, events []interface{}) error
	// GetUnprocessed returns up to limit unprocessed outbox messages.
	GetUnprocessed(ctx context.Context, limit int) ([]*Message, error)
	// MarkProcessed marks a message as successfully published.
	MarkProcessed(ctx context.Context, id uuid.UUID) error
	// IncrementRetry increments the retry counter for a failed message.
	IncrementRetry(ctx context.Context, id uuid.UUID) error
}

// PostgresRepository implements Repository using PostgreSQL.
type PostgresRepository struct {
	pool *pgxpool.Pool
}

// NewPostgresRepository creates a new PostgreSQL-backed outbox repository.
func NewPostgresRepository(pool *pgxpool.Pool) *PostgresRepository {
	return &PostgresRepository{pool: pool}
}

// SaveEvents translates domain events to outbox messages and persists them
// within the provided transaction (ensures atomicity with domain changes).
func (r *PostgresRepository) SaveEvents(ctx context.Context, tx pgx.Tx, events []interface{}) error {
	for _, event := range events {
		msg, err := toOutboxMessage(event)
		if err != nil {
			return fmt.Errorf("convert event to outbox message: %w", err)
		}
		if msg == nil {
			continue // event type not mapped to an integration event
		}

		_, err = tx.Exec(ctx, `
			INSERT INTO outbox_messages (id, aggregate_id, type, payload, processed, created_at, retry_count)
			VALUES ($1, $2, $3, $4, false, $5, 0)`,
			msg.ID,
			msg.AggregateID,
			msg.Type,
			msg.Payload,
			msg.CreatedAt,
		)
		if err != nil {
			return fmt.Errorf("insert outbox message: %w", err)
		}
	}
	return nil
}

// GetUnprocessed retrieves pending outbox messages for the background publisher.
func (r *PostgresRepository) GetUnprocessed(ctx context.Context, limit int) ([]*Message, error) {
	rows, err := r.pool.Query(ctx, `
		SELECT id, aggregate_id, type, payload, processed, created_at, processed_at, retry_count
		FROM outbox_messages
		WHERE processed = false AND retry_count < 5
		ORDER BY created_at ASC
		LIMIT $1`, limit)
	if err != nil {
		return nil, fmt.Errorf("query outbox messages: %w", err)
	}
	defer rows.Close()

	var messages []*Message
	for rows.Next() {
		var m Message
		if err := rows.Scan(
			&m.ID, &m.AggregateID, &m.Type, &m.Payload,
			&m.Processed, &m.CreatedAt, &m.ProcessedAt, &m.RetryCount,
		); err != nil {
			return nil, fmt.Errorf("scan outbox message: %w", err)
		}
		messages = append(messages, &m)
	}
	return messages, rows.Err()
}

// MarkProcessed marks an outbox message as successfully published.
func (r *PostgresRepository) MarkProcessed(ctx context.Context, id uuid.UUID) error {
	now := time.Now().UTC()
	_, err := r.pool.Exec(ctx, `
		UPDATE outbox_messages SET processed = true, processed_at = $2 WHERE id = $1`, id, now)
	return err
}

// IncrementRetry increments the retry counter for a failed message.
func (r *PostgresRepository) IncrementRetry(ctx context.Context, id uuid.UUID) error {
	_, err := r.pool.Exec(ctx, `
		UPDATE outbox_messages SET retry_count = retry_count + 1 WHERE id = $1`, id)
	return err
}

// toOutboxMessage translates a domain event to an integration event outbox entry.
// Only events that need to be published externally are translated.
func toOutboxMessage(event interface{}) (*Message, error) {
	switch e := event.(type) {
	case domain.PaymentCompletedEvent:
		integrationEvent := messaging.PaymentCompletedIntegrationEvent{
			PaymentID:     e.PaymentID.String(),
			AppointmentID: e.AppointmentID.String(),
			Status:        "Completed",
			TransactionID: e.TransactionID,
		}
		payload, err := json.Marshal(integrationEvent)
		if err != nil {
			return nil, err
		}
		return &Message{
			ID:          uuid.New(),
			AggregateID: e.PaymentID,
			Type:        "PaymentCompletedIntegrationEvent",
			Payload:     payload,
			CreatedAt:   time.Now().UTC(),
		}, nil

	case domain.PaymentFailedEvent:
		integrationEvent := messaging.PaymentFailedIntegrationEvent{
			PaymentID:     e.PaymentID.String(),
			AppointmentID: e.AppointmentID.String(),
			Reason:        e.Reason,
		}
		payload, err := json.Marshal(integrationEvent)
		if err != nil {
			return nil, err
		}
		return &Message{
			ID:          uuid.New(),
			AggregateID: e.PaymentID,
			Type:        "PaymentFailedIntegrationEvent",
			Payload:     payload,
			CreatedAt:   time.Now().UTC(),
		}, nil

	default:
		// Not all domain events need to be published externally (e.g. PaymentCreatedEvent)
		return nil, nil
	}
}
