// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package factory

import (
	"fmt"

	log "github.com/sirupsen/logrus"

	"github.com/datadog/stickerlandia/sticker-award/internal/config"
	"github.com/datadog/stickerlandia/sticker-award/internal/messaging"
	"github.com/datadog/stickerlandia/sticker-award/internal/messaging/aws"
	"github.com/datadog/stickerlandia/sticker-award/internal/messaging/kafka"
)

// NewEventPublisher creates an event publisher based on the configured messaging provider.
// This factory enables runtime selection between different messaging transports:
// - kafka: Apache Kafka via IBM Sarama
// - aws: AWS EventBridge
//
// The returned publisher implements the messaging.EventPublisher interface,
// allowing the domain layer to remain transport-agnostic.
func NewEventPublisher(cfg *config.Config) (messaging.EventPublisher, error) {
	log.WithFields(log.Fields{
		"provider": cfg.MessagingProvider,
	}).Info("Creating event publisher")

	switch cfg.MessagingProvider {
	case config.MessagingProviderKafka:
		producer, err := kafka.NewProducer(&cfg.Kafka)
		if err != nil {
			return nil, fmt.Errorf("failed to create Kafka producer: %w", err)
		}
		log.WithFields(log.Fields{
			"provider": "kafka",
			"brokers":  cfg.Kafka.Brokers,
		}).Info("Kafka event publisher created successfully")
		return producer, nil

	case config.MessagingProviderAWS:
		producer, err := aws.NewEventBridgeProducer(&cfg.AWS)
		if err != nil {
			return nil, fmt.Errorf("failed to create AWS EventBridge producer: %w", err)
		}
		log.WithFields(log.Fields{
			"provider": "aws",
			"region":   cfg.AWS.Region,
			"busName":  cfg.AWS.EventBusName,
		}).Info("AWS EventBridge producer created successfully")
		return producer, nil

	default:
		return nil, fmt.Errorf("unsupported messaging provider: %s (supported: kafka, aws)", cfg.MessagingProvider)
	}
}
