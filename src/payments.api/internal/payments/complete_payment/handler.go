package completepayment

import (
	"context"
	"fmt"
	"log/slog"

	"github.com/google/uuid"
	"github.com/smart-health/payments-api/internal/payments/domain"
	"github.com/smart-health/payments-api/internal/payments/infrastructure"
	stripeservice "github.com/smart-health/payments-api/internal/stripe"
)

// ---------------------------------------------------------------------------
// Command
// ---------------------------------------------------------------------------

// Command carries the PaymentID of the payment to process via Stripe.
type Command struct {
	PaymentID uuid.UUID
}

// Result is returned after processing the payment.
type Result struct {
	PaymentID     string
	Status        string
	TransactionID string
}

// ---------------------------------------------------------------------------
// Handler
// ---------------------------------------------------------------------------

// Handler handles the CompletePaymentCommand.
//
// Flow:
//  1. Load Payment aggregate.
//  2. Call Stripe to create a PaymentIntent.
//  3. On success: MarkProcessing → MarkCompleted → persist (outbox populated).
//  4. On Stripe error: MarkFailed → persist (outbox populated).
//
// The PaymentCompletedEvent domain event is translated to
// PaymentCompletedIntegrationEvent by the outbox repository.
type Handler struct {
	repo          infrastructure.PaymentRepository
	stripeService stripeservice.Service
	logger        *slog.Logger
}

// NewHandler creates a new CompletePaymentHandler.
func NewHandler(repo infrastructure.PaymentRepository, stripe stripeservice.Service, logger *slog.Logger) *Handler {
	return &Handler{repo: repo, stripeService: stripe, logger: logger}
}

// Handle processes the command.
func (h *Handler) Handle(ctx context.Context, cmd Command) (*Result, error) {
	payment, err := h.repo.FindByID(ctx, cmd.PaymentID)
	if err != nil {
		return nil, fmt.Errorf("find payment: %w", err)
	}
	if payment == nil {
		return nil, &domain.ErrPaymentNotFound{ID: cmd.PaymentID}
	}

	h.logger.Info("processing Stripe payment",
		"paymentId", payment.ID,
		"appointmentId", payment.AppointmentID,
		"amount", payment.Amount,
		"currency", payment.Currency)

	// Call Stripe to create a PaymentIntent
	intentID, stripeErr := h.stripeService.CreatePaymentIntent(
		ctx, payment.Amount, payment.Currency, payment.AppointmentID)

	if stripeErr != nil {
		h.logger.Error("Stripe error, marking payment as failed",
			"paymentId", payment.ID,
			"error", stripeErr)

		if err := payment.MarkFailed(fmt.Sprintf("Stripe error: %v", stripeErr)); err != nil {
			return nil, fmt.Errorf("mark payment failed: %w", err)
		}
	} else {
		// Mark processing (intent created), then mark completed (auto-confirm in test mode)
		if err := payment.MarkProcessing(intentID); err != nil {
			return nil, fmt.Errorf("mark payment processing: %w", err)
		}
		if err := payment.MarkCompleted(); err != nil {
			return nil, fmt.Errorf("mark payment completed: %w", err)
		}

		h.logger.Info("payment completed",
			"paymentId", payment.ID,
			"transactionId", intentID)
	}

	// Persist changes – outbox messages are populated by the repository
	if err := h.repo.Update(ctx, payment); err != nil {
		return nil, fmt.Errorf("update payment: %w", err)
	}

	return &Result{
		PaymentID:     payment.ID.String(),
		Status:        payment.Status.String(),
		TransactionID: payment.StripePaymentIntentID,
	}, nil
}
