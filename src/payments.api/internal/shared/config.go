package shared

import (
	"log/slog"
	"os"
	"strconv"
)

// Config holds all service configuration loaded from environment variables.
// This follows the 12-factor app methodology: config from environment.
type Config struct {
	// Server
	Port        string
	Environment string

	// Database
	DatabaseURL string

	// Messaging
	RabbitMQURL       string
	UseInMemoryBroker bool
	IncomingQueue     string // queue to consume AppointmentSlotReserved events from
	OutgoingExchange  string // exchange to publish PaymentCompleted events to

	// Stripe (use sk_test_* for test mode)
	StripeSecretKey string
}

// LoadConfig reads config from environment variables with defaults.
func LoadConfig() *Config {
	return &Config{
		Port:              getEnv("PORT", "8080"),
		Environment:       getEnv("ENVIRONMENT", "development"),
		DatabaseURL:       getEnv("DATABASE_URL", "postgres://postgres:postgres@localhost:5432/smarthealth_payments?sslmode=disable"),
		RabbitMQURL:       getEnv("RABBITMQ_URL", "amqp://guest:guest@localhost:5672/"),
		UseInMemoryBroker: getBoolEnv("USE_IN_MEMORY_BROKER", true),
		IncomingQueue:     getEnv("INCOMING_QUEUE", "appointment.slot.reserved"),
		OutgoingExchange:  getEnv("OUTGOING_EXCHANGE", "payment.completed"),
		StripeSecretKey:   getEnv("STRIPE_SECRET_KEY", "sk_test_placeholder"),
	}
}

func getEnv(key, defaultVal string) string {
	if v := os.Getenv(key); v != "" {
		return v
	}
	return defaultVal
}

func getBoolEnv(key string, defaultVal bool) bool {
	v := os.Getenv(key)
	if v == "" {
		return defaultVal
	}
	b, err := strconv.ParseBool(v)
	if err != nil {
		slog.Warn("invalid bool env var, using default", "key", key, "value", v)
		return defaultVal
	}
	return b
}
