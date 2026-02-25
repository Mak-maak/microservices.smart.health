package createpayment_test

import (
	"testing"

	"github.com/google/uuid"
	createpayment "github.com/smart-health/payments-api/internal/payments/create_payment"
)

func TestCommand_Validate_Valid(t *testing.T) {
	cmd := createpayment.Command{
		AppointmentID: uuid.New(),
		UserID:        "user-1",
		Amount:        100.0,
		Currency:      "usd",
	}
	if err := cmd.Validate(); err != nil {
		t.Errorf("unexpected validation error: %v", err)
	}
}

func TestCommand_Validate_MissingAppointmentID(t *testing.T) {
	cmd := createpayment.Command{
		UserID:   "user-1",
		Amount:   100.0,
		Currency: "usd",
	}
	if err := cmd.Validate(); err == nil {
		t.Error("expected validation error for missing appointmentId")
	}
}

func TestCommand_Validate_ZeroAmount(t *testing.T) {
	cmd := createpayment.Command{
		AppointmentID: uuid.New(),
		UserID:        "user-1",
		Amount:        0,
		Currency:      "usd",
	}
	if err := cmd.Validate(); err == nil {
		t.Error("expected validation error for zero amount")
	}
}

func TestCommand_Validate_InvalidCurrency(t *testing.T) {
	cmd := createpayment.Command{
		AppointmentID: uuid.New(),
		UserID:        "user-1",
		Amount:        100.0,
		Currency:      "us", // should be 3 chars
	}
	if err := cmd.Validate(); err == nil {
		t.Error("expected validation error for invalid currency")
	}
}
