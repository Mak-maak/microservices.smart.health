package messaging

// ---------------------------------------------------------------------------
// Incoming integration events (consumed by this service)
// ---------------------------------------------------------------------------

// AppointmentSlotReservedEvent is published by the Appointments service
// when a time slot is successfully reserved. This triggers payment processing.
type AppointmentSlotReservedEvent struct {
	AppointmentID string  `json:"appointmentId"`
	UserID        string  `json:"userId"`
	Amount        float64 `json:"amount"`
	Currency      string  `json:"currency"`
}

// ---------------------------------------------------------------------------
// Outgoing integration events (published by this service via Outbox)
// ---------------------------------------------------------------------------

// PaymentCompletedIntegrationEvent is published when payment succeeds.
// The Appointments service consumes this to confirm the booking.
type PaymentCompletedIntegrationEvent struct {
	PaymentID     string `json:"paymentId"`
	AppointmentID string `json:"appointmentId"`
	Status        string `json:"status"`
	TransactionID string `json:"transactionId"`
}

// PaymentFailedIntegrationEvent is published when payment fails,
// enabling compensating transactions in the Appointments service.
type PaymentFailedIntegrationEvent struct {
	PaymentID     string `json:"paymentId"`
	AppointmentID string `json:"appointmentId"`
	Reason        string `json:"reason"`
}
