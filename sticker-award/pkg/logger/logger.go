package logger

import (
	"fmt"

	"go.uber.org/zap"
)

// New creates a new zap logger with the specified level and format
func New(level, format string) (*zap.Logger, error) {
	var config zap.Config

	switch format {
	case "json":
		config = zap.NewProductionConfig()
	case "console":
		config = zap.NewDevelopmentConfig()
	default:
		config = zap.NewProductionConfig()
	}

	// Parse log level
	parsedLevel, err := zap.ParseAtomicLevel(level)
	if err != nil {
		return nil, fmt.Errorf("invalid log level %s: %w", level, err)
	}

	config.Level = parsedLevel

	// Build logger
	logger, err := config.Build(
		zap.AddCallerSkip(1),              // Skip one caller level for wrapper functions
		zap.AddStacktrace(zap.ErrorLevel), // Add stack trace for errors
	)
	if err != nil {
		return nil, fmt.Errorf("failed to build logger: %w", err)
	}

	return logger, nil
}
