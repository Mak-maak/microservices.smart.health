package createpayment

import (
	"context"
	"fmt"
	"log/slog"

	"github.com/google/uuid"
	completepayment "github.com/smart-health/payments-api/internal/payments/complete_payment"
	"github.com/smart-health/payments-api/internal/payments/domain"
	"github.com/smart-health/payments-api/internal/payments/infrastructure"
	"github.com/smart-health/payments-api/internal/shared"
)

// ---------------------------------------------------------------------------
// Command
// ---------------------------------------------------------------------------

// Command carries the data needed to create a new payment.
// Triggered by consuming the AppointmentSlotReservedEvent from the message broker.
type Command struct {
	AppointmentID uuid.UUID
	UserID        string
	Amount        float64
	Currency      string
}

// Result is returned after successfully creating (or idempotently finding) a payment.
type Result struct {
	PaymentID string
	Status    string
}

// ---------------------------------------------------------------------------
// Validation
// ---------------------------------------------------------------------------

// Validate checks that the command fields satisfy business rules.
func (c Command) Validate() error {
	if c.AppointmentID == uuid.Nil {
		return fmt.Errorf("appointmentId is required")
	}
	if c.UserID == "" {
		return fmt.Errorf("userId is required")
	}
	if c.Amount <= 0 {
		return fmt.Errorf("amount must be greater than zero")
	}
	if len(c.Currency) != 3 {
		return fmt.Errorf("currency must be a 3-letter ISO code")
	}
	return nil
}

// ---------------------------------------------------------------------------
// Handler
// ---------------------------------------------------------------------------

// Handler handles the CreatePaymentCommand.
//
// Flow:
//  1. Validate command.
//  2. Idempotency check – return existing payment if one already exists for this appointment.
//  3. Create Payment aggregate.
//  4. Persist payment (and PaymentCreatedEvent to outbox).
//  5. Dispatch CompletePaymentCommand to process the Stripe charge.
type Handler struct {
	repo     infrastructure.PaymentRepository
	mediator *shared.Mediator
	logger   *slog.Logger
}

// NewHandler creates a new CreatePaymentHandler.
func NewHandler(repo infrastructure.PaymentRepository, mediator *shared.Mediator, logger *slog.Logger) *Handler {
	return &Handler{repo: repo, mediator: mediator, logger: logger}
}

// Handle processes the command and returns the result.
func (h *Handler) Handle(ctx context.Context, cmd Command) (*Result, error) {
	if err := cmd.Validate(); err != nil {
		return nil, fmt.Errorf("validation error: %w", err)
	}

	// Idempotency check: skip creation if payment already exists for this appointment.
	// This ensures that duplicate AppointmentSlotReserved events are safely ignored.
	existing, err := h.repo.FindByAppointmentID(ctx, cmd.AppointmentID)
	if err != nil {
		return nil, fmt.Errorf("check existing payment: %w", err)
	}
	if existing != nil {
		h.logger.Info("payment already exists for appointment, skipping",
			"appointmentId", cmd.AppointmentID,
			"paymentId", existing.ID)
		return &Result{PaymentID: existing.ID.String(), Status: existing.Status.String()}, nil
	}

	// Create new Payment aggregate
	payment, err := domain.NewPayment(cmd.AppointmentID, cmd.UserID, cmd.Amount, cmd.Currency)
	if err != nil {
		return nil, fmt.Errorf("create payment aggregate: %w", err)
	}

	// Persist (includes domain events in outbox)
	if err := h.repo.Create(ctx, payment); err != nil {
		return nil, fmt.Errorf("persist payment: %w", err)
	}

	h.logger.Info("payment created",
		"paymentId", payment.ID,
		"appointmentId", payment.AppointmentID)

	// Dispatch CompletePaymentCommand – processes the Stripe charge
	completeCmd := completepayment.Command{PaymentID: payment.ID}
	resp, err := h.mediator.Send(ctx, completeCmd)
	if err != nil {
		// Log but don't fail – the outbox will eventually retry
		h.logger.Error("complete payment command failed",
			"paymentId", payment.ID,
			"error", err)
		return &Result{PaymentID: payment.ID.String(), Status: payment.Status.String()}, nil
	}

	if result, ok := resp.(*completepayment.Result); ok {
		return &Result{PaymentID: result.PaymentID, Status: result.Status}, nil
	}

	return &Result{PaymentID: payment.ID.String(), Status: payment.Status.String()}, nil
}
