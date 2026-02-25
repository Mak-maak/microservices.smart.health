package database

import (
	"context"
	"fmt"
	"time"

	"github.com/jackc/pgx/v5/pgxpool"
)

// NewPostgresPool creates a new PostgreSQL connection pool with sensible defaults.
// It retries up to maxAttempts times to handle database startup delays (e.g. in Docker).
func NewPostgresPool(ctx context.Context, dsn string) (*pgxpool.Pool, error) {
	const maxAttempts = 10
	const retryDelay = 3 * time.Second

	config, err := pgxpool.ParseConfig(dsn)
	if err != nil {
		return nil, fmt.Errorf("parse DSN: %w", err)
	}

	// Connection pool settings
	config.MaxConns = 25
	config.MinConns = 2
	config.MaxConnLifetime = 30 * time.Minute
	config.MaxConnIdleTime = 5 * time.Minute

	var pool *pgxpool.Pool
	for attempt := 1; attempt <= maxAttempts; attempt++ {
		pool, err = pgxpool.NewWithConfig(ctx, config)
		if err == nil {
			// Ping to verify connectivity
			if pingErr := pool.Ping(ctx); pingErr == nil {
				return pool, nil
			} else {
				pool.Close()
				err = pingErr
			}
		}

		if attempt < maxAttempts {
			time.Sleep(retryDelay)
		}
	}

	return nil, fmt.Errorf("connect to postgres after %d attempts: %w", maxAttempts, err)
}
