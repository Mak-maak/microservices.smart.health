# =============================================================================
# Multi-stage Dockerfile for SmartHealth Payments API (Go)
# Non-root user, port 8080
# =============================================================================

# --- Build stage ---
FROM golang:1.23-alpine AS build
WORKDIR /app

# Install git for go get
RUN apk add --no-cache git

# Cache dependencies
COPY go.mod go.sum ./
RUN go mod download

# Copy source and build
COPY . ./
RUN CGO_ENABLED=0 GOOS=linux GOARCH=amd64 \
    go build -ldflags="-s -w" -o /app/payments-api ./cmd/api

# --- Runtime stage ---
FROM alpine:3.20 AS runtime
WORKDIR /app

RUN apk add --no-cache ca-certificates curl

# Create non-root user
RUN addgroup -S -g 1001 appgroup && \
    adduser -S -u 1001 -G appgroup appuser

# Copy binary and migrations
COPY --from=build /app/payments-api ./
COPY migrations/ ./migrations/

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

USER appuser
EXPOSE 8080

ENV PORT=8080
ENV ENVIRONMENT=production

ENTRYPOINT ["/app/payments-api"]
