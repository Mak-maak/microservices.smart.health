package shared

import (
	"context"
	"fmt"
)

// -----------------------------------------------------------------------
// Lightweight Mediator – Architectural Decision
//
// Rather than reflection-based dispatch (like C#'s MediatR), this mediator
// uses explicit handler registration with a type-switch. This is idiomatic
// Go: explicit over implicit, no magic, easy to trace, and zero overhead.
//
// Usage:
//
//	m := NewMediator(createHandler, completeHandler, getHandler)
//	result, err := m.Send(ctx, CreatePaymentCommand{...})
// -----------------------------------------------------------------------

// Request is a marker interface for commands and queries.
type Request interface{}

// Response is the result of handling a request.
type Response interface{}

// MediatorHandler is a function that handles a request and returns a response.
type MediatorHandler func(ctx context.Context, req Request) (Response, error)

// Mediator dispatches commands and queries to their registered handlers.
// This decouples the HTTP transport layer from business logic.
type Mediator struct {
	handlers map[string]MediatorHandler
}

// NewMediator creates a new Mediator with no handlers registered.
func NewMediator() *Mediator {
	return &Mediator{handlers: make(map[string]MediatorHandler)}
}

// Register adds a handler for a specific request type (identified by type name).
func (m *Mediator) Register(typeName string, handler MediatorHandler) {
	m.handlers[typeName] = handler
}

// Send dispatches a request to its registered handler.
func (m *Mediator) Send(ctx context.Context, req Request) (Response, error) {
	typeName := fmt.Sprintf("%T", req)
	handler, ok := m.handlers[typeName]
	if !ok {
		return nil, fmt.Errorf("no handler registered for request type %s", typeName)
	}
	return handler(ctx, req)
}
