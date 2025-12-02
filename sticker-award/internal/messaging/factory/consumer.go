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

// NewMessageConsumer creates a message consumer based on the configured messaging provider.
// This factory enables runtime selection between different messaging transports:
// - kafka: Apache Kafka consumer groups via IBM Sarama
// - aws: AWS SQS (not yet implemented)
//
// The returned consumer implements the messaging.MessageConsumer interface,
// allowing the application to remain transport-agnostic.
func NewMessageConsumer(cfg *config.Config) (messaging.MessageConsumer, error) {
	log.WithFields(log.Fields{
		"provider": cfg.MessagingProvider,
	}).Info("Creating message consumer")

	switch cfg.MessagingProvider {
	case config.MessagingProviderKafka:
		consumer, err := kafka.NewConsumer(&cfg.Kafka, cfg.ServiceName)
		if err != nil {
			return nil, fmt.Errorf("failed to create Kafka consumer: %w", err)
		}
		log.WithFields(log.Fields{
			"provider":    "kafka",
			"brokers":     cfg.Kafka.Brokers,
			"groupId":     cfg.Kafka.GroupID,
			"serviceName": cfg.ServiceName,
		}).Info("Kafka message consumer created successfully")
		return consumer, nil

	case config.MessagingProviderAWS:
		consumer, err := aws.NewSQSConsumer(&cfg.AWS, cfg.ServiceName)
		if err != nil {
			return nil, fmt.Errorf("failed to create AWS SQS consumer: %w", err)
		}
		log.WithFields(log.Fields{
			"provider":    "aws",
			"region":      cfg.AWS.Region,
			"queueURL":    cfg.AWS.SQSQueueURL,
			"serviceName": cfg.ServiceName,
		}).Info("AWS SQS consumer created successfully")
		return consumer, nil

	default:
		return nil, fmt.Errorf("unsupported messaging provider: %s (supported: kafka, aws)", cfg.MessagingProvider)
	}
}
