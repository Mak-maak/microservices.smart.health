package stripe

import (
	"context"

	"github.com/google/uuid"
)

// Service defines the Stripe payment processing contract.
// This abstraction enables testing without hitting the Stripe API.
type Service interface {
	// CreatePaymentIntent creates a Stripe PaymentIntent in test mode.
	// Returns the PaymentIntent ID on success.
	CreatePaymentIntent(ctx context.Context, amount float64, currency string, appointmentID uuid.UUID) (string, error)
}
