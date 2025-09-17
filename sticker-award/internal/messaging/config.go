// Copyright 2025 Datadog, Inc.
// SPDX-License-Identifier: Apache-2.0

package messaging

import (
	"time"

	"github.com/IBM/sarama"
	"github.com/datadog/stickerlandia/sticker-award/internal/config"
)

// NewSaramaConfig creates a shared Sarama configuration for both producer and consumer
func NewSaramaConfig(clientID string) *sarama.Config {
	config := sarama.NewConfig()

	// Common configuration
	config.Version = sarama.V2_1_0_0
	config.ClientID = clientID

	// Network timeouts
	config.Net.DialTimeout = 10 * time.Second
	config.Net.ReadTimeout = 10 * time.Second
	config.Net.WriteTimeout = 10 * time.Second

	return config
}

// ConfigureProducer adds producer-specific configuration
func ConfigureProducer(config *sarama.Config, cfg *config.KafkaConfig) {
	config.Producer.Return.Successes = true
	config.Producer.Return.Errors = true
	config.Producer.RequiredAcks = sarama.RequiredAcks(cfg.RequireAcks)
	config.Producer.Retry.Max = cfg.ProducerRetries
	config.Producer.Timeout = time.Duration(cfg.ProducerTimeout) * time.Millisecond
	config.Producer.Compression = sarama.CompressionSnappy
	config.Producer.Flush.Bytes = cfg.ProducerBatchSize
	config.Producer.Flush.Frequency = 100 * time.Millisecond
}

// ConfigureConsumer adds consumer-specific configuration
func ConfigureConsumer(config *sarama.Config) {
	config.Consumer.Group.Rebalance.Strategy = sarama.NewBalanceStrategyRoundRobin()
	config.Consumer.Offsets.Initial = sarama.OffsetOldest
	config.Consumer.Return.Errors = true
	// Remove any consumer interceptors for now
	config.Consumer.Interceptors = []sarama.ConsumerInterceptor{}
}
