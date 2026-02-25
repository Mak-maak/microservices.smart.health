package main

import (
	"context"
	"errors"
	"fmt"
	"log/slog"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/gin-gonic/gin"
	"github.com/google/uuid"
	"github.com/jackc/pgx/v5/pgxpool"
	"github.com/smart-health/payments-api/internal/database"
	"github.com/smart-health/payments-api/internal/messaging"
	"github.com/smart-health/payments-api/internal/outbox"
	completepayment "github.com/smart-health/payments-api/internal/payments/complete_payment"
	createpayment "github.com/smart-health/payments-api/internal/payments/create_payment"
	"github.com/smart-health/payments-api/internal/payments/domain"
	getpayment "github.com/smart-health/payments-api/internal/payments/get_payment"
	"github.com/smart-health/payments-api/internal/payments/infrastructure"
	"github.com/smart-health/payments-api/internal/shared"
	stripeservice "github.com/smart-health/payments-api/internal/stripe"
)

func main() {
	// ----------------------------------------------------------------
	// Structured logging (JSON in production, text in development)
	// ----------------------------------------------------------------
	logger := newLogger()
	slog.SetDefault(logger)

	// ----------------------------------------------------------------
	// Configuration
	// ----------------------------------------------------------------
	cfg := shared.LoadConfig()
	logger.Info("starting SmartHealth Payments API",
		"port", cfg.Port,
		"environment", cfg.Environment,
		"useInMemoryBroker", cfg.UseInMemoryBroker)

	// ----------------------------------------------------------------
	// Database
	// ----------------------------------------------------------------
	ctx := context.Background()
	pool, err := database.NewPostgresPool(ctx, cfg.DatabaseURL)
	if err != nil {
		logger.Error("failed to connect to database", "error", err)
		os.Exit(1)
	}
	defer pool.Close()
	logger.Info("connected to PostgreSQL")

	// Run migrations on startup (development / staging)
	if err := runMigrations(ctx, pool, logger); err != nil {
		logger.Error("failed to run migrations", "error", err)
		os.Exit(1)
	}

	// ----------------------------------------------------------------
	// Infrastructure: Repositories, Stripe, Mediator
	// ----------------------------------------------------------------
	outboxRepo := outbox.NewPostgresRepository(pool)
	paymentRepo := infrastructure.NewPostgresPaymentRepository(pool, outboxRepo)
	stripeClient := stripeservice.NewStripeService(cfg.StripeSecretKey, logger)
	mediator := shared.NewMediator()

	// ----------------------------------------------------------------
	// Feature handlers (Vertical Slices)
	// ----------------------------------------------------------------
	completeHandler := completepayment.NewHandler(paymentRepo, stripeClient, logger)
	createHandler := createpayment.NewHandler(paymentRepo, mediator, logger)
	getHandler := getpayment.NewHandler(paymentRepo)

	// Register handlers in mediator
	mediator.Register(
		fmt.Sprintf("%T", createpayment.Command{}),
		func(ctx context.Context, req shared.Request) (shared.Response, error) {
			return createHandler.Handle(ctx, req.(createpayment.Command))
		},
	)
	mediator.Register(
		fmt.Sprintf("%T", completepayment.Command{}),
		func(ctx context.Context, req shared.Request) (shared.Response, error) {
			return completeHandler.Handle(ctx, req.(completepayment.Command))
		},
	)
	mediator.Register(
		fmt.Sprintf("%T", getpayment.Query{}),
		func(ctx context.Context, req shared.Request) (shared.Response, error) {
			return getHandler.Handle(ctx, req.(getpayment.Query))
		},
	)

	// ----------------------------------------------------------------
	// Messaging: Publisher + Consumer
	// ----------------------------------------------------------------
	var publisher messaging.Publisher
	var consumer messaging.Consumer

	if cfg.UseInMemoryBroker {
		publisher = messaging.NewInMemoryPublisher(logger)
		consumer = messaging.NewInMemoryConsumer(logger)
	} else {
		publisher, err = messaging.NewRabbitMQPublisher(cfg.RabbitMQURL, cfg.OutgoingExchange, logger)
		if err != nil {
			logger.Error("failed to create RabbitMQ publisher", "error", err)
			os.Exit(1)
		}
		defer publisher.Close()

		consumer, err = messaging.NewRabbitMQConsumer(cfg.RabbitMQURL, cfg.IncomingQueue, logger)
		if err != nil {
			logger.Error("failed to create RabbitMQ consumer", "error", err)
			os.Exit(1)
		}
		defer consumer.Close()
	}

	// ----------------------------------------------------------------
	// Outbox background worker
	// ----------------------------------------------------------------
	outboxWorker := outbox.NewWorker(outboxRepo, publisher, logger)

	// ----------------------------------------------------------------
	// HTTP server (Gin)
	// ----------------------------------------------------------------
	if cfg.Environment == "production" {
		gin.SetMode(gin.ReleaseMode)
	}

	router := gin.New()
	router.Use(gin.Recovery())
	router.Use(ginLogger(logger))

	// Health endpoints
	router.GET("/health", func(c *gin.Context) {
		c.JSON(http.StatusOK, gin.H{"status": "healthy"})
	})
	router.GET("/readiness", func(c *gin.Context) {
		if err := pool.Ping(c.Request.Context()); err != nil {
			c.JSON(http.StatusServiceUnavailable, gin.H{"status": "not ready", "error": err.Error()})
			return
		}
		c.JSON(http.StatusOK, gin.H{"status": "ready"})
	})
	router.GET("/liveness", func(c *gin.Context) {
		c.JSON(http.StatusOK, gin.H{"status": "alive", "timestamp": time.Now().UTC()})
	})

	// Payments API
	api := router.Group("/api/payments")
	{
		// GET /api/payments/:id – get payment by ID
		api.GET("/:id", func(c *gin.Context) {
			id, err := uuid.Parse(c.Param("id"))
			if err != nil {
				c.JSON(http.StatusBadRequest, gin.H{"error": "invalid payment id"})
				return
			}

			resp, err := mediator.Send(c.Request.Context(), getpayment.Query{PaymentID: id})
			if err != nil {
				var notFound *domain.ErrPaymentNotFound
				if errors.As(err, &notFound) {
					c.JSON(http.StatusNotFound, gin.H{"error": err.Error()})
					return
				}
				c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
				return
			}

			c.JSON(http.StatusOK, resp)
		})

		// POST /api/payments/trigger – manual trigger for dev/testing
		// In production this is driven by AppointmentSlotReserved events
		api.POST("/trigger", func(c *gin.Context) {
			var req struct {
				AppointmentID string  `json:"appointmentId" binding:"required"`
				UserID        string  `json:"userId"        binding:"required"`
				Amount        float64 `json:"amount"        binding:"required,gt=0"`
				Currency      string  `json:"currency"      binding:"required,len=3"`
			}
			if err := c.ShouldBindJSON(&req); err != nil {
				c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
				return
			}

			appointmentID, err := uuid.Parse(req.AppointmentID)
			if err != nil {
				c.JSON(http.StatusBadRequest, gin.H{"error": "invalid appointmentId"})
				return
			}

			resp, err := mediator.Send(c.Request.Context(), createpayment.Command{
				AppointmentID: appointmentID,
				UserID:        req.UserID,
				Amount:        req.Amount,
				Currency:      req.Currency,
			})
			if err != nil {
				c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
				return
			}

			c.JSON(http.StatusCreated, resp)
		})
	}

	// ----------------------------------------------------------------
	// Start background goroutines
	// ----------------------------------------------------------------
	appCtx, cancel := context.WithCancel(ctx)
	defer cancel()

	// Outbox worker
	go outboxWorker.Run(appCtx)

	// Message consumer
	go func() {
		if err := consumer.Start(appCtx, func(ctx context.Context, event messaging.AppointmentSlotReservedEvent) error {
			appointmentID, err := uuid.Parse(event.AppointmentID)
			if err != nil {
				return fmt.Errorf("invalid appointmentId in event: %w", err)
			}

			cmd := createpayment.Command{
				AppointmentID: appointmentID,
				UserID:        event.UserID,
				Amount:        event.Amount,
				Currency:      event.Currency,
			}

			if _, err := mediator.Send(ctx, cmd); err != nil {
				return fmt.Errorf("handle AppointmentSlotReserved: %w", err)
			}
			return nil
		}); err != nil {
			logger.Error("consumer error", "error", err)
		}
	}()

	// ----------------------------------------------------------------
	// HTTP server with graceful shutdown
	// ----------------------------------------------------------------
	srv := &http.Server{
		Addr:         ":" + cfg.Port,
		Handler:      router,
		ReadTimeout:  15 * time.Second,
		WriteTimeout: 15 * time.Second,
		IdleTimeout:  60 * time.Second,
	}

	// Run server in a goroutine
	go func() {
		logger.Info("HTTP server listening", "addr", srv.Addr)
		if err := srv.ListenAndServe(); err != nil && !errors.Is(err, http.ErrServerClosed) {
			logger.Error("server error", "error", err)
			os.Exit(1)
		}
	}()

	// Wait for OS signal (SIGINT / SIGTERM)
	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)
	<-quit

	logger.Info("shutdown signal received, draining...")

	// Graceful shutdown
	shutdownCtx, shutdownCancel := context.WithTimeout(context.Background(), 30*time.Second)
	defer shutdownCancel()

	cancel() // stop background workers
	if err := srv.Shutdown(shutdownCtx); err != nil {
		logger.Error("server forced to shutdown", "error", err)
	}

	logger.Info("server exited")
}

// runMigrations applies the database schema on startup.
func runMigrations(ctx context.Context, pool *pgxpool.Pool, logger *slog.Logger) error {
	logger.Info("running database migrations")
	migrations := `
	CREATE TABLE IF NOT EXISTS payments (
		id                       UUID          PRIMARY KEY,
		appointment_id           UUID          NOT NULL UNIQUE,
		user_id                  VARCHAR(256)  NOT NULL,
		amount                   NUMERIC(18,2) NOT NULL,
		currency                 VARCHAR(3)    NOT NULL,
		status                   INT           NOT NULL DEFAULT 0,
		stripe_payment_intent_id VARCHAR(255),
		failure_reason           VARCHAR(1000),
		created_at               TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
		updated_at               TIMESTAMPTZ
	);
	CREATE INDEX IF NOT EXISTS idx_payments_appointment_id ON payments(appointment_id);
	CREATE INDEX IF NOT EXISTS idx_payments_status ON payments(status);
	CREATE TABLE IF NOT EXISTS outbox_messages (
		id           UUID         PRIMARY KEY,
		aggregate_id UUID         NOT NULL,
		type         VARCHAR(255) NOT NULL,
		payload      JSONB        NOT NULL,
		processed    BOOLEAN      NOT NULL DEFAULT false,
		created_at   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
		processed_at TIMESTAMPTZ,
		retry_count  INT          NOT NULL DEFAULT 0
	);
	CREATE INDEX IF NOT EXISTS idx_outbox_unprocessed ON outbox_messages(processed, retry_count, created_at) WHERE processed = false;
	`
	_, err := pool.Exec(ctx, migrations)
	return err
}

func newLogger() *slog.Logger {
	env := os.Getenv("ENVIRONMENT")
	if env == "production" {
		return slog.New(slog.NewJSONHandler(os.Stdout, &slog.HandlerOptions{
			Level: slog.LevelInfo,
		}))
	}
	return slog.New(slog.NewTextHandler(os.Stdout, &slog.HandlerOptions{
		Level: slog.LevelDebug,
	}))
}

func ginLogger(logger *slog.Logger) gin.HandlerFunc {
	return func(c *gin.Context) {
		start := time.Now()
		c.Next()
		logger.Info("http request",
			"method", c.Request.Method,
			"path", c.Request.URL.Path,
			"status", c.Writer.Status(),
			"duration_ms", time.Since(start).Milliseconds(),
		)
	}
}
