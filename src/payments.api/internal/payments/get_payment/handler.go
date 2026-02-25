package getpayment

import (
	"context"
	"fmt"
	"time"

	"github.com/google/uuid"
	"github.com/smart-health/payments-api/internal/payments/domain"
	"github.com/smart-health/payments-api/internal/payments/infrastructure"
)

// ---------------------------------------------------------------------------
// Query
// ---------------------------------------------------------------------------

// Query is the read-side request for payment details.
type Query struct {
	PaymentID uuid.UUID
}

// Result is the read model returned to the caller.
type Result struct {
	PaymentID             string     `json:"paymentId"`
	AppointmentID         string     `json:"appointmentId"`
	UserID                string     `json:"userId"`
	Amount                float64    `json:"amount"`
	Currency              string     `json:"currency"`
	Status                string     `json:"status"`
	StripePaymentIntentID string     `json:"stripePaymentIntentId,omitempty"`
	FailureReason         string     `json:"failureReason,omitempty"`
	CreatedAt             time.Time  `json:"createdAt"`
	UpdatedAt             *time.Time `json:"updatedAt,omitempty"`
}

// ---------------------------------------------------------------------------
// Handler
// ---------------------------------------------------------------------------

// Handler handles the GetPaymentQuery.
type Handler struct {
	repo infrastructure.PaymentRepository
}

// NewHandler creates a new GetPaymentHandler.
func NewHandler(repo infrastructure.PaymentRepository) *Handler {
	return &Handler{repo: repo}
}

// Handle processes the query and returns the payment read model.
func (h *Handler) Handle(ctx context.Context, q Query) (*Result, error) {
	payment, err := h.repo.FindByID(ctx, q.PaymentID)
	if err != nil {
		return nil, fmt.Errorf("find payment: %w", err)
	}
	if payment == nil {
		return nil, &domain.ErrPaymentNotFound{ID: q.PaymentID}
	}

	return &Result{
		PaymentID:             payment.ID.String(),
		AppointmentID:         payment.AppointmentID.String(),
		UserID:                payment.UserID,
		Amount:                payment.Amount,
		Currency:              payment.Currency,
		Status:                payment.Status.String(),
		StripePaymentIntentID: payment.StripePaymentIntentID,
		FailureReason:         payment.FailureReason,
		CreatedAt:             payment.CreatedAt,
		UpdatedAt:             payment.UpdatedAt,
	}, nil
}
