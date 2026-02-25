package stripe

import (
	"context"
	"fmt"
	"log/slog"

	"github.com/google/uuid"
	stripego "github.com/stripe/stripe-go/v81"
	"github.com/stripe/stripe-go/v81/paymentintent"
)

// StripeService implements the Service interface using the Stripe Go SDK.
//
// Architectural Decision: The Stripe SDK is wrapped in this service to:
//  1. Isolate Stripe-specific types from the domain layer.
//  2. Enable mocking in tests via the Service interface.
//  3. Centralise Stripe SDK configuration (API key, logging).
type StripeService struct {
	secretKey string
	logger    *slog.Logger
}

// NewStripeService creates a new Stripe service configured with the given secret key.
// Use a "sk_test_*" key for test mode.
func NewStripeService(secretKey string, logger *slog.Logger) *StripeService {
	stripego.Key = secretKey
	return &StripeService{secretKey: secretKey, logger: logger}
}

// CreatePaymentIntent creates a Stripe PaymentIntent for the given amount and currency.
// Amount is in the major currency unit (e.g. 10.00 for $10.00 USD).
// Returns the PaymentIntent ID on success.
func (s *StripeService) CreatePaymentIntent(ctx context.Context, amount float64, currency string, appointmentID uuid.UUID) (string, error) {
	// Stripe uses the smallest currency unit (cents for USD)
	amountCents := int64(amount * 100)

	params := &stripego.PaymentIntentParams{
		Amount:   stripego.Int64(amountCents),
		Currency: stripego.String(currency),
		AutomaticPaymentMethods: &stripego.PaymentIntentAutomaticPaymentMethodsParams{
			Enabled: stripego.Bool(true),
		},
		Metadata: map[string]string{
			"appointmentId": appointmentID.String(),
			"service":       "smarthealth-payments",
		},
		Description: stripego.String(fmt.Sprintf("SmartHealth appointment payment for %s", appointmentID)),
	}

	s.logger.Info("creating Stripe PaymentIntent",
		"appointmentId", appointmentID,
		"amount", amount,
		"currency", currency)

	intent, err := paymentintent.New(params)
	if err != nil {
		s.logger.Error("Stripe PaymentIntent creation failed",
			"appointmentId", appointmentID,
			"error", err)
		return "", fmt.Errorf("stripe CreatePaymentIntent: %w", err)
	}

	s.logger.Info("Stripe PaymentIntent created",
		"intentId", intent.ID,
		"appointmentId", appointmentID)

	return intent.ID, nil
}
