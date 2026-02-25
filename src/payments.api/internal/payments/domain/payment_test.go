package domain_test

import (
	"testing"

	"github.com/google/uuid"
	"github.com/smart-health/payments-api/internal/payments/domain"
)

func TestNewPayment_Valid(t *testing.T) {
	p, err := domain.NewPayment(uuid.New(), "user-1", 100.0, "usd")
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if p.Status != domain.PaymentStatusPending {
		t.Errorf("expected Pending, got %v", p.Status)
	}
	if len(p.DomainEvents()) != 1 {
		t.Errorf("expected 1 domain event, got %d", len(p.DomainEvents()))
	}
}

func TestNewPayment_InvalidAmount(t *testing.T) {
	_, err := domain.NewPayment(uuid.New(), "user-1", 0, "usd")
	if err == nil {
		t.Fatal("expected error for zero amount")
	}
}

func TestNewPayment_NegativeAmount(t *testing.T) {
	_, err := domain.NewPayment(uuid.New(), "user-1", -50, "usd")
	if err == nil {
		t.Fatal("expected error for negative amount")
	}
}

func TestNewPayment_EmptyCurrency(t *testing.T) {
	_, err := domain.NewPayment(uuid.New(), "user-1", 100, "")
	if err == nil {
		t.Fatal("expected error for empty currency")
	}
}

func TestPayment_MarkProcessing(t *testing.T) {
	p, _ := domain.NewPayment(uuid.New(), "user-1", 100.0, "usd")
	if err := p.MarkProcessing("pi_test_123"); err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if p.Status != domain.PaymentStatusProcessing {
		t.Errorf("expected Processing, got %v", p.Status)
	}
	if p.StripePaymentIntentID != "pi_test_123" {
		t.Errorf("expected pi_test_123, got %v", p.StripePaymentIntentID)
	}
}

func TestPayment_MarkCompleted(t *testing.T) {
	p, _ := domain.NewPayment(uuid.New(), "user-1", 100.0, "usd")
	_ = p.MarkProcessing("pi_test_123")
	if err := p.MarkCompleted(); err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if p.Status != domain.PaymentStatusCompleted {
		t.Errorf("expected Completed, got %v", p.Status)
	}
	// Should have PaymentCreatedEvent + PaymentCompletedEvent
	events := p.DomainEvents()
	if len(events) != 2 {
		t.Errorf("expected 2 domain events, got %d", len(events))
	}
	if _, ok := events[1].(domain.PaymentCompletedEvent); !ok {
		t.Errorf("expected PaymentCompletedEvent as second event")
	}
}

func TestPayment_MarkFailed(t *testing.T) {
	p, _ := domain.NewPayment(uuid.New(), "user-1", 100.0, "usd")
	if err := p.MarkFailed("card declined"); err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if p.Status != domain.PaymentStatusFailed {
		t.Errorf("expected Failed, got %v", p.Status)
	}
	if p.FailureReason != "card declined" {
		t.Errorf("unexpected failure reason: %v", p.FailureReason)
	}
}

func TestPayment_InvalidTransition(t *testing.T) {
	p, _ := domain.NewPayment(uuid.New(), "user-1", 100.0, "usd")
	_ = p.MarkFailed("error")
	if err := p.MarkCompleted(); err == nil {
		t.Fatal("expected error transitioning from Failed to Completed")
	}
}

func TestPayment_ClearDomainEvents(t *testing.T) {
	p, _ := domain.NewPayment(uuid.New(), "user-1", 100.0, "usd")
	p.ClearDomainEvents()
	if len(p.DomainEvents()) != 0 {
		t.Errorf("expected 0 domain events after clear, got %d", len(p.DomainEvents()))
	}
}
