package outbox

import (
	"context"
	"log/slog"
	"time"

	"github.com/smart-health/payments-api/internal/messaging"
)

const (
	maxRetries      = 5
	pollingInterval = 5 * time.Second
	batchSize       = 50
)

// Worker is a background service that polls the outbox table and publishes
// pending messages to the message broker.
//
// Architectural Decision: The outbox worker decouples message publishing
// from business operations. Even if the broker is temporarily unavailable,
// messages accumulate in the DB and are published once the broker recovers.
// This provides at-least-once delivery semantics.
type Worker struct {
	repo      Repository
	publisher messaging.Publisher
	logger    *slog.Logger
}

// NewWorker creates a new outbox background worker.
func NewWorker(repo Repository, publisher messaging.Publisher, logger *slog.Logger) *Worker {
	return &Worker{repo: repo, publisher: publisher, logger: logger}
}

// Run starts the outbox polling loop. It blocks until ctx is cancelled.
// Designed to be run as a goroutine.
func (w *Worker) Run(ctx context.Context) {
	w.logger.Info("outbox worker starting")

	ticker := time.NewTicker(pollingInterval)
	defer ticker.Stop()

	for {
		select {
		case <-ctx.Done():
			w.logger.Info("outbox worker stopped")
			return
		case <-ticker.C:
			w.processBatch(ctx)
		}
	}
}

func (w *Worker) processBatch(ctx context.Context) {
	messages, err := w.repo.GetUnprocessed(ctx, batchSize)
	if err != nil {
		w.logger.Error("failed to fetch outbox messages", "error", err)
		return
	}

	if len(messages) == 0 {
		return
	}

	w.logger.Info("processing outbox messages", "count", len(messages))

	for _, msg := range messages {
		if err := w.publisher.Publish(ctx, msg.Type, msg.Payload); err != nil {
			w.logger.Error("failed to publish outbox message",
				"id", msg.ID,
				"type", msg.Type,
				"attempt", msg.RetryCount+1,
				"error", err)

			if err := w.repo.IncrementRetry(ctx, msg.ID); err != nil {
				w.logger.Error("failed to increment retry count", "id", msg.ID, "error", err)
			}

			if msg.RetryCount+1 >= maxRetries {
				w.logger.Error("outbox message exceeded max retries, dead-lettering",
					"id", msg.ID, "type", msg.Type)
			}
			continue
		}

		if err := w.repo.MarkProcessed(ctx, msg.ID); err != nil {
			w.logger.Error("failed to mark outbox message as processed", "id", msg.ID, "error", err)
		} else {
			w.logger.Info("outbox message published", "id", msg.ID, "type", msg.Type)
		}
	}
}
