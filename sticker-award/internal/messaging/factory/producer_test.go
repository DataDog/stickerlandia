// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package factory

import (
	"testing"

	"github.com/datadog/stickerlandia/sticker-award/internal/config"
	"github.com/datadog/stickerlandia/sticker-award/internal/messaging/aws"
	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"
)

func TestNewEventPublisher_AWS(t *testing.T) {
	cfg := &config.Config{
		MessagingProvider: config.MessagingProviderAWS,
		AWS: config.AWSConfig{
			Region:       "us-east-1",
			EventBusName: "test-bus",
		},
	}

	producer, err := NewEventPublisher(cfg)

	require.NoError(t, err)
	require.NotNil(t, producer)

	// Verify it's an AWS EventBridge producer by type assertion
	_, ok := producer.(*aws.EventBridgeProducer)
	assert.True(t, ok, "Expected AWS EventBridge producer but got different type")
}

func TestNewEventPublisher_InvalidProvider(t *testing.T) {
	cfg := &config.Config{
		MessagingProvider: "invalid",
		Kafka: config.KafkaConfig{
			Brokers: []string{"localhost:9092"},
		},
	}

	producer, err := NewEventPublisher(cfg)

	assert.Error(t, err)
	assert.Nil(t, producer)
	assert.Contains(t, err.Error(), "unsupported messaging provider")
}

func TestNewEventPublisher_EmptyProvider(t *testing.T) {
	cfg := &config.Config{
		MessagingProvider: "",
		Kafka: config.KafkaConfig{
			Brokers: []string{"localhost:9092"},
		},
	}

	producer, err := NewEventPublisher(cfg)

	// Empty provider should be treated as invalid
	assert.Error(t, err)
	assert.Nil(t, producer)
}

func TestNewEventPublisher_KafkaFailsWithInvalidConfig(t *testing.T) {
	cfg := &config.Config{
		MessagingProvider: config.MessagingProviderKafka,
		Kafka: config.KafkaConfig{
			Brokers: []string{}, // Empty brokers should fail
		},
	}

	producer, err := NewEventPublisher(cfg)

	assert.Error(t, err)
	assert.Nil(t, producer)
}

func TestNewEventPublisher_PassesConfigToAWS(t *testing.T) {
	testRegion := "eu-west-1"
	testBusName := "production-events"

	cfg := &config.Config{
		MessagingProvider: config.MessagingProviderAWS,
		AWS: config.AWSConfig{
			Region:       testRegion,
			EventBusName: testBusName,
		},
	}

	producer, err := NewEventPublisher(cfg)

	require.NoError(t, err)
	require.NotNil(t, producer)

	// Verify configuration was passed correctly
	awsProducer, ok := producer.(*aws.EventBridgeProducer)
	require.True(t, ok)
	assert.NotNil(t, awsProducer)
}
