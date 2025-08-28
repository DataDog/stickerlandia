package factory

import (
	"context"

	"github.com/IBM/sarama"
	"github.com/datadog/stickerlandia/sticker-award/internal/messaging/middleware"
)

// MessageHandler defines the interface for handling Kafka messages
type MessageHandler interface {
	Handle(ctx context.Context, message *sarama.ConsumerMessage) error
	Topic() string
}

// HandlerFactory creates message handlers with the standard middleware stack applied
type HandlerFactory struct {
	serviceName string
}

// NewHandlerFactory creates a new handler factory
func NewHandlerFactory(serviceName string) *HandlerFactory {
	return &HandlerFactory{
		serviceName: serviceName,
	}
}

// CreateCloudEventHandler creates a CloudEvent handler with complete messaging middleware:
// Single middleware handles: DSM tracking + Root trace creation + CloudEvent parsing
func CreateCloudEventHandler[T any](
	f *HandlerFactory,
	handler middleware.CloudEventMessageHandler[T],
	operationName string,
) MessageHandler {
	// Single messaging middleware handles all concerns
	return middleware.NewMessagingHandler(handler, operationName, f.serviceName)
}

// GetServiceName returns the service name used by this factory
func (f *HandlerFactory) GetServiceName() string {
	return f.serviceName
}
