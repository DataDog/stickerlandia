// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package config

import (
	"fmt"
	"strings"

	"github.com/spf13/viper"
)

// Config holds all configuration for the application
type Config struct {
	Server    ServerConfig    `mapstructure:"server"`
	Database  DatabaseConfig  `mapstructure:"database"`
	Kafka     KafkaConfig     `mapstructure:"kafka"`
	Catalogue CatalogueConfig `mapstructure:"catalogue"`
	Logging   LoggingConfig   `mapstructure:"logging"`
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

	return &config, nil
}

// setDefaults sets default configuration values
func setDefaults() {
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

	// Catalogue service defaults
	viper.SetDefault("catalogue.base_url", "http://localhost:8080")
	viper.SetDefault("catalogue.timeout", 30)

	// Logging defaults
	viper.SetDefault("logging.level", "info")
	viper.SetDefault("logging.format", "json")
}

// ConnectionString returns the database connection string
func (d *DatabaseConfig) ConnectionString() string {
	return fmt.Sprintf("host=%s port=%d user=%s password=%s dbname=%s sslmode=%s",
		d.Host, d.Port, d.User, d.Password, d.Name, d.SSLMode)
}
