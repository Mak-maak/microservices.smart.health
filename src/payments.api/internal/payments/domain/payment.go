package domain

import (
	"errors"
	"fmt"
	"time"

	"github.com/google/uuid"
)

// ErrInvalidAmount is returned when a payment amount is invalid.
var ErrInvalidAmount = errors.New("payment amount must be greater than zero")

// ErrInvalidCurrency is returned when currency is empty.
var ErrInvalidCurrency = errors.New("currency must be specified")

// ErrInvalidTransition is returned when a state transition is not allowed.
type ErrInvalidTransition struct {
	From    PaymentStatus
	Allowed []PaymentStatus
}

func (e *ErrInvalidTransition) Error() string {
	return fmt.Sprintf("cannot transition from %s; allowed statuses: %v", e.From, e.Allowed)
}

// ErrPaymentNotFound is returned when a payment record cannot be found.
type ErrPaymentNotFound struct {
	ID uuid.UUID
}

func (e *ErrPaymentNotFound) Error() string {
	return fmt.Sprintf("payment %s was not found", e.ID)
}

// ErrDuplicatePayment is returned when a payment already exists for an appointment.
type ErrDuplicatePayment struct {
	AppointmentID uuid.UUID
}

func (e *ErrDuplicatePayment) Error() string {
	return fmt.Sprintf("payment for appointment %s already exists", e.AppointmentID)
}

// -----------------------------------------------------------------------
// Payment aggregate root
//
// Architectural Decision: The Payment aggregate encapsulates all business
// rules and state transitions. External code cannot directly mutate state;
// it must go through the aggregate's methods.
// Domain events are collected in-memory and processed by the repository
// to populate the outbox table in the same database transaction.
// -----------------------------------------------------------------------

// Payment is the aggregate root for the payments bounded context.
type Payment struct {
	ID                    uuid.UUID
	AppointmentID         uuid.UUID
	UserID                string
	Amount                float64
	Currency              string
	Status                PaymentStatus
	StripePaymentIntentID string
	FailureReason         string
	CreatedAt             time.Time
	UpdatedAt             *time.Time

	// Domain events collected during this operation (not persisted directly).
	// Processed by the repository layer to populate the outbox.
	domainEvents []interface{}
}

// NewPayment is the factory function – enforces invariants on creation.
func NewPayment(appointmentID uuid.UUID, userID string, amount float64, currency string) (*Payment, error) {
	if amount <= 0 {
		return nil, ErrInvalidAmount
	}
	if currency == "" {
		return nil, ErrInvalidCurrency
	}

	p := &Payment{
		ID:            uuid.New(),
		AppointmentID: appointmentID,
		UserID:        userID,
		Amount:        amount,
		Currency:      lowercase(currency),
		Status:        PaymentStatusPending,
		CreatedAt:     time.Now().UTC(),
	}

	p.addEvent(PaymentCreatedEvent{
		PaymentID:     p.ID,
		AppointmentID: appointmentID,
		UserID:        userID,
		Amount:        amount,
		Currency:      currency,
	})

	return p, nil
}

// MarkProcessing transitions the payment to Processing after Stripe intent is created.
func (p *Payment) MarkProcessing(stripeIntentID string) error {
	if err := p.ensureStatus(PaymentStatusPending); err != nil {
		return err
	}
	p.StripePaymentIntentID = stripeIntentID
	p.Status = PaymentStatusProcessing
	now := time.Now().UTC()
	p.UpdatedAt = &now
	return nil
}

// MarkCompleted transitions the payment to Completed and raises PaymentCompletedEvent.
// The PaymentCompletedEvent will be translated into an outbox message by the repository.
func (p *Payment) MarkCompleted() error {
	if err := p.ensureStatus(PaymentStatusProcessing, PaymentStatusPending); err != nil {
		return err
	}
	p.Status = PaymentStatusCompleted
	now := time.Now().UTC()
	p.UpdatedAt = &now

	p.addEvent(PaymentCompletedEvent{
		PaymentID:     p.ID,
		AppointmentID: p.AppointmentID,
		TransactionID: p.StripePaymentIntentID,
	})
	return nil
}

// MarkFailed transitions the payment to Failed and raises PaymentFailedEvent.
func (p *Payment) MarkFailed(reason string) error {
	if err := p.ensureStatus(PaymentStatusProcessing, PaymentStatusPending); err != nil {
		return err
	}
	p.FailureReason = reason
	p.Status = PaymentStatusFailed
	now := time.Now().UTC()
	p.UpdatedAt = &now

	p.addEvent(PaymentFailedEvent{
		PaymentID:     p.ID,
		AppointmentID: p.AppointmentID,
		Reason:        reason,
	})
	return nil
}

// DomainEvents returns the collected domain events (read-only copy).
func (p *Payment) DomainEvents() []interface{} {
	result := make([]interface{}, len(p.domainEvents))
	copy(result, p.domainEvents)
	return result
}

// ClearDomainEvents removes all collected domain events.
func (p *Payment) ClearDomainEvents() {
	p.domainEvents = nil
}

func (p *Payment) addEvent(event interface{}) {
	p.domainEvents = append(p.domainEvents, event)
}

func (p *Payment) ensureStatus(allowed ...PaymentStatus) error {
	for _, s := range allowed {
		if p.Status == s {
			return nil
		}
	}
	return &ErrInvalidTransition{From: p.Status, Allowed: allowed}
}

func lowercase(s string) string {
	result := make([]byte, len(s))
	for i := range s {
		c := s[i]
		if c >= 'A' && c <= 'Z' {
			c += 'a' - 'A'
		}
		result[i] = c
	}
	return string(result)
}
