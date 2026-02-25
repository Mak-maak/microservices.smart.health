package infrastructure

import (
	"context"
	"fmt"
	"time"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"
	"github.com/smart-health/payments-api/internal/outbox"
	"github.com/smart-health/payments-api/internal/payments/domain"
)

// PaymentRepository defines the persistence contract for Payment aggregates.
// Using the repository pattern isolates the domain from infrastructure concerns.
type PaymentRepository interface {
	Create(ctx context.Context, payment *domain.Payment) error
	FindByID(ctx context.Context, id uuid.UUID) (*domain.Payment, error)
	FindByAppointmentID(ctx context.Context, appointmentID uuid.UUID) (*domain.Payment, error)
	Update(ctx context.Context, payment *domain.Payment) error
}

// PostgresPaymentRepository implements PaymentRepository using PostgreSQL.
// It uses a pgxpool for connection pooling and handles transactions internally.
type PostgresPaymentRepository struct {
	pool       *pgxpool.Pool
	outboxRepo outbox.Repository
}

// NewPostgresPaymentRepository creates a new PostgreSQL-backed payment repository.
func NewPostgresPaymentRepository(pool *pgxpool.Pool, outboxRepo outbox.Repository) *PostgresPaymentRepository {
	return &PostgresPaymentRepository{pool: pool, outboxRepo: outboxRepo}
}

// Create persists a new Payment aggregate in a transaction that also
// writes any domain events to the outbox table (transactional outbox pattern).
func (r *PostgresPaymentRepository) Create(ctx context.Context, payment *domain.Payment) error {
	return r.withTransaction(ctx, func(tx pgx.Tx) error {
		_, err := tx.Exec(ctx, `
			INSERT INTO payments (id, appointment_id, user_id, amount, currency, status, stripe_payment_intent_id, failure_reason, created_at, updated_at)
			VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10)`,
			payment.ID,
			payment.AppointmentID,
			payment.UserID,
			payment.Amount,
			payment.Currency,
			int(payment.Status),
			nilIfEmpty(payment.StripePaymentIntentID),
			nilIfEmpty(payment.FailureReason),
			payment.CreatedAt,
			payment.UpdatedAt,
		)
		if err != nil {
			return fmt.Errorf("insert payment: %w", err)
		}

		// Write domain events to outbox in the same transaction
		if err := r.outboxRepo.SaveEvents(ctx, tx, payment.DomainEvents()); err != nil {
			return fmt.Errorf("save outbox events: %w", err)
		}

		payment.ClearDomainEvents()
		return nil
	})
}

// FindByID retrieves a payment by its primary key.
func (r *PostgresPaymentRepository) FindByID(ctx context.Context, id uuid.UUID) (*domain.Payment, error) {
	row := r.pool.QueryRow(ctx, `
		SELECT id, appointment_id, user_id, amount, currency, status,
		       COALESCE(stripe_payment_intent_id, ''), COALESCE(failure_reason, ''), created_at, updated_at
		FROM payments WHERE id = $1`, id)

	return scanPayment(row)
}

// FindByAppointmentID retrieves a payment by its appointment ID (for idempotency checks).
func (r *PostgresPaymentRepository) FindByAppointmentID(ctx context.Context, appointmentID uuid.UUID) (*domain.Payment, error) {
	row := r.pool.QueryRow(ctx, `
		SELECT id, appointment_id, user_id, amount, currency, status,
		       COALESCE(stripe_payment_intent_id, ''), COALESCE(failure_reason, ''), created_at, updated_at
		FROM payments WHERE appointment_id = $1`, appointmentID)

	p, err := scanPayment(row)
	if err != nil {
		return nil, err
	}
	return p, nil
}

// Update persists state changes to an existing payment and writes domain events to outbox.
func (r *PostgresPaymentRepository) Update(ctx context.Context, payment *domain.Payment) error {
	return r.withTransaction(ctx, func(tx pgx.Tx) error {
		_, err := tx.Exec(ctx, `
			UPDATE payments SET
				status = $2,
				stripe_payment_intent_id = $3,
				failure_reason = $4,
				updated_at = $5
			WHERE id = $1`,
			payment.ID,
			int(payment.Status),
			nilIfEmpty(payment.StripePaymentIntentID),
			nilIfEmpty(payment.FailureReason),
			payment.UpdatedAt,
		)
		if err != nil {
			return fmt.Errorf("update payment: %w", err)
		}

		// Write domain events to outbox in the same transaction
		if err := r.outboxRepo.SaveEvents(ctx, tx, payment.DomainEvents()); err != nil {
			return fmt.Errorf("save outbox events: %w", err)
		}

		payment.ClearDomainEvents()
		return nil
	})
}

func (r *PostgresPaymentRepository) withTransaction(ctx context.Context, fn func(pgx.Tx) error) error {
	tx, err := r.pool.Begin(ctx)
	if err != nil {
		return fmt.Errorf("begin transaction: %w", err)
	}

	if err := fn(tx); err != nil {
		_ = tx.Rollback(ctx)
		return err
	}

	return tx.Commit(ctx)
}

func scanPayment(row pgx.Row) (*domain.Payment, error) {
	var p domain.Payment
	var updatedAt *time.Time
	var status int

	err := row.Scan(
		&p.ID,
		&p.AppointmentID,
		&p.UserID,
		&p.Amount,
		&p.Currency,
		&status,
		&p.StripePaymentIntentID,
		&p.FailureReason,
		&p.CreatedAt,
		&updatedAt,
	)
	if err != nil {
		if err == pgx.ErrNoRows {
			return nil, nil
		}
		return nil, fmt.Errorf("scan payment: %w", err)
	}

	p.Status = domain.PaymentStatus(status)
	p.UpdatedAt = updatedAt
	return &p, nil
}

func nilIfEmpty(s string) *string {
	if s == "" {
		return nil
	}
	return &s
}
