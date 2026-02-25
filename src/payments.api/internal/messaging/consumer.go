package messaging

import (
	"context"
	"encoding/json"
	"log/slog"

	amqp "github.com/rabbitmq/amqp091-go"
)

// MessageHandler is a function that processes a consumed message.
type MessageHandler func(ctx context.Context, event AppointmentSlotReservedEvent) error

// Consumer defines the contract for consuming integration events.
type Consumer interface {
	Start(ctx context.Context, handler MessageHandler) error
	Close() error
}

// ---------------------------------------------------------------------------
// RabbitMQ Consumer
// ---------------------------------------------------------------------------

// RabbitMQConsumer consumes messages from a RabbitMQ queue.
type RabbitMQConsumer struct {
	conn   *amqp.Connection
	ch     *amqp.Channel
	queue  string
	logger *slog.Logger
}

// NewRabbitMQConsumer creates a new RabbitMQ consumer for the given queue.
func NewRabbitMQConsumer(url, queue string, logger *slog.Logger) (*RabbitMQConsumer, error) {
	conn, err := amqp.Dial(url)
	if err != nil {
		return nil, err
	}

	ch, err := conn.Channel()
	if err != nil {
		conn.Close()
		return nil, err
	}

	// Declare the queue (idempotent)
	if _, err := ch.QueueDeclare(queue, true, false, false, false, nil); err != nil {
		ch.Close()
		conn.Close()
		return nil, err
	}

	return &RabbitMQConsumer{conn: conn, ch: ch, queue: queue, logger: logger}, nil
}

// Start begins consuming messages and dispatches them to the handler.
// Blocks until ctx is cancelled.
func (c *RabbitMQConsumer) Start(ctx context.Context, handler MessageHandler) error {
	msgs, err := c.ch.Consume(c.queue, "", false, false, false, false, nil)
	if err != nil {
		return err
	}

	c.logger.Info("RabbitMQ consumer started", "queue", c.queue)

	for {
		select {
		case <-ctx.Done():
			return nil
		case msg, ok := <-msgs:
			if !ok {
				return nil
			}
			c.handleMessage(ctx, msg, handler)
		}
	}
}

func (c *RabbitMQConsumer) handleMessage(ctx context.Context, msg amqp.Delivery, handler MessageHandler) {
	var event AppointmentSlotReservedEvent
	if err := json.Unmarshal(msg.Body, &event); err != nil {
		c.logger.Error("failed to unmarshal AppointmentSlotReservedEvent", "error", err)
		_ = msg.Nack(false, false) // dead-letter
		return
	}

	if err := handler(ctx, event); err != nil {
		c.logger.Error("failed to handle AppointmentSlotReservedEvent",
			"appointmentId", event.AppointmentID, "error", err)
		_ = msg.Nack(false, true) // requeue
		return
	}

	_ = msg.Ack(false)
}

// Close releases the consumer resources.
func (c *RabbitMQConsumer) Close() error {
	if err := c.ch.Close(); err != nil {
		return err
	}
	return c.conn.Close()
}

// ---------------------------------------------------------------------------
// In-Memory (No-Op) Consumer – for local dev and testing
// ---------------------------------------------------------------------------

// InMemoryConsumer is a no-op consumer that never receives messages.
// In development mode, payments are triggered directly via HTTP.
type InMemoryConsumer struct {
	logger *slog.Logger
}

// NewInMemoryConsumer creates a new in-memory consumer.
func NewInMemoryConsumer(logger *slog.Logger) *InMemoryConsumer {
	return &InMemoryConsumer{logger: logger}
}

func (c *InMemoryConsumer) Start(ctx context.Context, _ MessageHandler) error {
	c.logger.Info("in-memory consumer started (no-op) – use HTTP endpoints to trigger payments")
	<-ctx.Done()
	return nil
}

func (c *InMemoryConsumer) Close() error { return nil }
