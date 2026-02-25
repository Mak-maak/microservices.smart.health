package domain

import "github.com/google/uuid"

// Domain events raised by the Payment aggregate.
// These are stored in the Outbox and published as integration events.

// PaymentCreatedEvent is raised when a new payment record is created.
type PaymentCreatedEvent struct {
	PaymentID     uuid.UUID
	AppointmentID uuid.UUID
	UserID        string
	Amount        float64
	Currency      string
}

// PaymentCompletedEvent is raised when payment is successfully completed.
// This will be translated to PaymentCompletedIntegrationEvent for the outbox.
type PaymentCompletedEvent struct {
	PaymentID     uuid.UUID
	AppointmentID uuid.UUID
	TransactionID string // Stripe PaymentIntent ID
}

// PaymentFailedEvent is raised when a payment fails.
type PaymentFailedEvent struct {
	PaymentID     uuid.UUID
	AppointmentID uuid.UUID
	Reason        string
}
