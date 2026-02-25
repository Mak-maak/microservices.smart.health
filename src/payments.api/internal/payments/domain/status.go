package domain

// PaymentStatus represents the lifecycle states of a payment.
// Transitions: Pending → Processing → Completed | Failed
type PaymentStatus int

const (
	PaymentStatusPending    PaymentStatus = 0
	PaymentStatusProcessing PaymentStatus = 1
	PaymentStatusCompleted  PaymentStatus = 2
	PaymentStatusFailed     PaymentStatus = 3
)

// String returns the string representation of the status.
func (s PaymentStatus) String() string {
	switch s {
	case PaymentStatusPending:
		return "Pending"
	case PaymentStatusProcessing:
		return "Processing"
	case PaymentStatusCompleted:
		return "Completed"
	case PaymentStatusFailed:
		return "Failed"
	default:
		return "Unknown"
	}
}
