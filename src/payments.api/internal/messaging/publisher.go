package messaging

import (
	"context"
	"log/slog"

	amqp "github.com/rabbitmq/amqp091-go"
)

// Publisher defines the contract for publishing integration events to a message broker.
type Publisher interface {
	Publish(ctx context.Context, routingKey string, payload []byte) error
	Close() error
}

// ---------------------------------------------------------------------------
// RabbitMQ Publisher
// ---------------------------------------------------------------------------

// RabbitMQPublisher publishes messages to RabbitMQ via AMQP.
type RabbitMQPublisher struct {
	conn     *amqp.Connection
	channel  *amqp.Channel
	exchange string
	logger   *slog.Logger
}

// NewRabbitMQPublisher creates a new RabbitMQ publisher connected to the given exchange.
func NewRabbitMQPublisher(url, exchange string, logger *slog.Logger) (*RabbitMQPublisher, error) {
	conn, err := amqp.Dial(url)
	if err != nil {
		return nil, err
	}

	ch, err := conn.Channel()
	if err != nil {
		conn.Close()
		return nil, err
	}

	// Declare the exchange (idempotent)
	if err := ch.ExchangeDeclare(exchange, "topic", true, false, false, false, nil); err != nil {
		ch.Close()
		conn.Close()
		return nil, err
	}

	return &RabbitMQPublisher{conn: conn, channel: ch, exchange: exchange, logger: logger}, nil
}

// Publish sends a message to the exchange with the given routing key.
func (p *RabbitMQPublisher) Publish(ctx context.Context, routingKey string, payload []byte) error {
	return p.channel.PublishWithContext(ctx, p.exchange, routingKey, false, false, amqp.Publishing{
		ContentType:  "application/json",
		Body:         payload,
		DeliveryMode: amqp.Persistent,
	})
}

// Close releases the RabbitMQ channel and connection.
func (p *RabbitMQPublisher) Close() error {
	if err := p.channel.Close(); err != nil {
		return err
	}
	return p.conn.Close()
}

// ---------------------------------------------------------------------------
// In-Memory (No-Op) Publisher – for local dev and testing
// ---------------------------------------------------------------------------

// InMemoryPublisher is a no-op publisher that logs messages instead of sending them.
// Use this in development/testing by setting USE_IN_MEMORY_BROKER=true.
type InMemoryPublisher struct {
	logger *slog.Logger
}

// NewInMemoryPublisher creates a new in-memory publisher.
func NewInMemoryPublisher(logger *slog.Logger) *InMemoryPublisher {
	return &InMemoryPublisher{logger: logger}
}

func (p *InMemoryPublisher) Publish(_ context.Context, routingKey string, payload []byte) error {
	p.logger.Info("in-memory publish (no-op)", "routingKey", routingKey, "payload", string(payload))
	return nil
}

func (p *InMemoryPublisher) Close() error { return nil }
