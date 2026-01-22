// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package config

import (
	"fmt"
	"net/url"
	"os"
	"strconv"
	"strings"

	"github.com/spf13/viper"
)

// MessagingProvider defines the messaging transport to use
type MessagingProvider string

const (
	MessagingProviderKafka MessagingProvider = "kafka"
	MessagingProviderAWS   MessagingProvider = "aws"
)

// Config holds all configuration for the application
type Config struct {
	ServiceName       string            `mapstructure:"service_name"`
	Server            ServerConfig      `mapstructure:"server"`
	Database          DatabaseConfig    `mapstructure:"database"`
	MessagingProvider MessagingProvider `mapstructure:"messaging_provider"`
	Kafka             KafkaConfig       `mapstructure:"kafka"`
	AWS               AWSConfig         `mapstructure:"aws"`
	Catalogue         CatalogueConfig   `mapstructure:"catalogue"`
	Logging           LoggingConfig     `mapstructure:"logging"`
	Auth              AuthConfig        `mapstructure:"auth"`
}

// AuthConfig holds JWT authentication configuration
type AuthConfig struct {
	Issuer    string `mapstructure:"issuer"`
	JwksUrl   string `mapstructure:"jwks_url"`
	ClockSkew int    `mapstructure:"clock_skew"`
}

// JWKSUrl returns the JWKS URL - uses explicit jwks_url if set, otherwise derives from issuer
func (a *AuthConfig) JWKSUrl() string {
	if a.JwksUrl != "" {
		return a.JwksUrl
	}
	return a.Issuer + "/.well-known/jwks"
}

// ServerConfig holds HTTP server configuration
type ServerConfig struct {
	Port string `mapstructure:"port"`
	Host string `mapstructure:"host"`
}

// DatabaseConfig holds database connection configuration
type DatabaseConfig struct {
	Host     string `mapstructure:"host"`
	Port     int    `mapstructure:"port"`
	User     string `mapstructure:"user"`
	Password string `mapstructure:"password"`
	Name     string `mapstructure:"name"`
	SSLMode  string `mapstructure:"ssl_mode"`
}

// KafkaConfig holds Kafka configuration
type KafkaConfig struct {
	Brokers   []string `mapstructure:"brokers"`
	GroupID   string   `mapstructure:"group_id"`
	EnableTls bool     `mapstructure:"enable_tls"`
	Username  string   `mapstructure:"username"`
	Password  string   `mapstructure:"password"`
	// Producer configuration
	ProducerTimeout   int  `mapstructure:"producer_timeout"`
	ProducerRetries   int  `mapstructure:"producer_retries"`
	ProducerBatchSize int  `mapstructure:"producer_batch_size"`
	RequireAcks       int  `mapstructure:"require_acks"`
	EnableIdempotent  bool `mapstructure:"enable_idempotent"`
}

// AWSConfig holds AWS messaging configuration (EventBridge + SQS)
// Field names match CDK environment variable names for direct binding
type AWSConfig struct {
	Region                 string `mapstructure:"region"`
	EventBusName           string `mapstructure:"event_bus_name"`            // EVENT_BUS_NAME from CDK
	UserRegisteredQueueURL string `mapstructure:"user_registered_queue_url"` // USER_REGISTERED_QUEUE_URL from CDK
	MaxConcurrency         int    `mapstructure:"max_concurrency"`
	VisibilityTimeout      int    `mapstructure:"visibility_timeout"` // Seconds
	WaitTimeSeconds        int    `mapstructure:"wait_time_seconds"`  // Long polling duration
}

// CatalogueConfig holds sticker catalogue service configuration
type CatalogueConfig struct {
	BaseURL string `mapstructure:"base_url"`
	Timeout int    `mapstructure:"timeout"`
}

// LoggingConfig holds logging configuration
type LoggingConfig struct {
	Level  string `mapstructure:"level"`
	Format string `mapstructure:"format"`
}

// Load loads configuration from environment variables and config files
func Load() (*Config, error) {
	viper.SetConfigName("config")
	viper.SetConfigType("yaml")
	viper.AddConfigPath(".")
	viper.AddConfigPath("./config")

	// Set default values
	setDefaults()

	// Enable reading from environment variables
	viper.AutomaticEnv()
	viper.SetEnvKeyReplacer(strings.NewReplacer(".", "_"))

	// Read config file (optional)
	if err := viper.ReadInConfig(); err != nil {
		if _, ok := err.(viper.ConfigFileNotFoundError); !ok {
			return nil, fmt.Errorf("failed to read config file: %w", err)
		}
	}

	var config Config
	if err := viper.Unmarshal(&config); err != nil {
		return nil, fmt.Errorf("failed to unmarshal config: %w", err)
	}

	// Override database config from DATABASE_URL if present
	if dbURL := os.Getenv("DATABASE_URL"); dbURL != "" {
		dbConfig, err := parseDatabaseURL(dbURL)
		if err != nil {
			return nil, fmt.Errorf("failed to parse DATABASE_URL: %w", err)
		}
		config.Database = *dbConfig
	}

	return &config, nil
}

// parseDatabaseURL parses a PostgreSQL connection URL into DatabaseConfig
// Supports format: postgres://user:password@host:port/dbname?sslmode=require
func parseDatabaseURL(dbURL string) (*DatabaseConfig, error) {
	u, err := url.Parse(dbURL)
	if err != nil {
		return nil, fmt.Errorf("invalid database URL: %w", err)
	}

	config := &DatabaseConfig{
		Host:    u.Hostname(),
		Port:    5432, // default
		SSLMode: "disable",
	}

	// Parse port
	if portStr := u.Port(); portStr != "" {
		port, err := strconv.Atoi(portStr)
		if err != nil {
			return nil, fmt.Errorf("invalid port: %w", err)
		}
		config.Port = port
	}

	// Parse user/password
	if u.User != nil {
		config.User = u.User.Username()
		if password, ok := u.User.Password(); ok {
			config.Password = password
		}
	}

	// Parse database name (remove leading slash)
	config.Name = strings.TrimPrefix(u.Path, "/")

	// Parse query parameters (sslmode, etc.)
	query := u.Query()
	if sslmode := query.Get("sslmode"); sslmode != "" {
		config.SSLMode = sslmode
	}

	return config, nil
}

// setDefaults sets default configuration values
func setDefaults() {
	// Service name default
	viper.SetDefault("service_name", "award-service")

	// Server defaults
	viper.SetDefault("server.port", "8080")
	viper.SetDefault("server.host", "localhost")

	// Database defaults
	viper.SetDefault("database.host", "localhost")
	viper.SetDefault("database.port", 5432)
	viper.SetDefault("database.user", "sticker_user")
	viper.SetDefault("database.password", "sticker_password")
	viper.SetDefault("database.name", "sticker_awards")
	viper.SetDefault("database.ssl_mode", "disable")

	// Messaging provider default (kafka for backward compatibility)
	viper.SetDefault("messaging_provider", "kafka")

	// Kafka defaults
	viper.SetDefault("kafka.brokers", []string{"localhost:9092"})
	viper.SetDefault("kafka.group_id", "sticker-award-service")
	viper.SetDefault("kafka.enable_tls", false)
	viper.SetDefault("kafka.username", "")
	viper.SetDefault("kafka.password", "")
	viper.SetDefault("kafka.producer_timeout", 5000) // 5 seconds in milliseconds
	viper.SetDefault("kafka.producer_retries", 3)
	viper.SetDefault("kafka.producer_batch_size", 16384) // 16KB
	viper.SetDefault("kafka.require_acks", 1)            // Wait for leader acknowledgment
	viper.SetDefault("kafka.enable_idempotent", true)

	// AWS defaults
	viper.SetDefault("aws.region", "us-east-1")
	viper.SetDefault("aws.event_bus_name", "")
	viper.SetDefault("aws.user_registered_queue_url", "")
	viper.SetDefault("aws.max_concurrency", 10)
	viper.SetDefault("aws.visibility_timeout", 30) // 30 seconds
	viper.SetDefault("aws.wait_time_seconds", 20)  // 20 seconds long polling

	// Explicit env var bindings for AWS config (CDK uses these exact names)
	_ = viper.BindEnv("aws.event_bus_name", "EVENT_BUS_NAME")
	_ = viper.BindEnv("aws.user_registered_queue_url", "USER_REGISTERED_QUEUE_URL")

	// Catalogue service defaults
	viper.SetDefault("catalogue.base_url", "http://localhost:8080")
	viper.SetDefault("catalogue.timeout", 30)

	// Logging defaults
	viper.SetDefault("logging.level", "info")
	viper.SetDefault("logging.format", "json")

	// Auth defaults - JWKS URL can be set separately from issuer for Docker networking
	viper.SetDefault("auth.issuer", "http://user-management:8080")
	viper.SetDefault("auth.jwks_url", "") // Empty means derive from issuer
	viper.SetDefault("auth.clock_skew", 30)

	// Explicit env var bindings for auth config
	_ = viper.BindEnv("auth.issuer", "OAUTH_ISSUER")
	_ = viper.BindEnv("auth.jwks_url", "OAUTH_JWKS_URL")
}

// ConnectionString returns the database connection string
func (d *DatabaseConfig) ConnectionString() string {
	return fmt.Sprintf("host=%s port=%d user=%s password=%s dbname=%s sslmode=%s",
		d.Host, d.Port, d.User, d.Password, d.Name, d.SSLMode)
}
