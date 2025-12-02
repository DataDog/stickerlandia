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

func TestNewMessageConsumer_AWS(t *testing.T) {
	cfg := &config.Config{
		MessagingProvider: config.MessagingProviderAWS,
		AWS: config.AWSConfig{
			Region:                 "us-east-1",
			UserRegisteredQueueURL: "https://sqs.us-east-1.amazonaws.com/123456789/test-queue",
		},
	}

	consumer, err := NewMessageConsumer(cfg)

	require.NoError(t, err)
	require.NotNil(t, consumer)

	// Verify it's an AWS SQS consumer by type assertion
	_, ok := consumer.(*aws.Consumer)
	assert.True(t, ok, "Expected AWS SQS consumer but got different type")
}

func TestNewMessageConsumer_InvalidProvider(t *testing.T) {
	cfg := &config.Config{
		MessagingProvider: "unsupported",
		Kafka: config.KafkaConfig{
			Brokers: []string{"localhost:9092"},
		},
	}

	consumer, err := NewMessageConsumer(cfg)

	assert.Error(t, err)
	assert.Nil(t, consumer)
	assert.Contains(t, err.Error(), "unsupported messaging provider")
}

func TestNewMessageConsumer_EmptyProvider(t *testing.T) {
	cfg := &config.Config{
		MessagingProvider: "",
		Kafka: config.KafkaConfig{
			Brokers: []string{"localhost:9092"},
		},
	}

	consumer, err := NewMessageConsumer(cfg)

	// Empty provider should be treated as invalid
	assert.Error(t, err)
	assert.Nil(t, consumer)
}

func TestNewMessageConsumer_KafkaFailsWithInvalidConfig(t *testing.T) {
	cfg := &config.Config{
		MessagingProvider: config.MessagingProviderKafka,
		Kafka: config.KafkaConfig{
			Brokers: []string{}, // Empty brokers should fail
			GroupID: "test-group",
		},
	}

	consumer, err := NewMessageConsumer(cfg)

	assert.Error(t, err)
	assert.Nil(t, consumer)
}

func TestNewMessageConsumer_PassesConfigToAWS(t *testing.T) {
	testRegion := "ap-southeast-1"
	testQueueURL := "https://sqs.ap-southeast-1.amazonaws.com/999/my-queue"

	cfg := &config.Config{
		MessagingProvider: config.MessagingProviderAWS,
		AWS: config.AWSConfig{
			Region:                 testRegion,
			UserRegisteredQueueURL: testQueueURL,
		},
	}

	consumer, err := NewMessageConsumer(cfg)

	require.NoError(t, err)
	require.NotNil(t, consumer)

	// Verify configuration was passed correctly
	sqsConsumer, ok := consumer.(*aws.Consumer)
	require.True(t, ok)
	assert.NotNil(t, sqsConsumer)
}

func TestNewMessageConsumer_DefaultValues(t *testing.T) {
	cfg := &config.Config{
		MessagingProvider: config.MessagingProviderAWS,
		AWS: config.AWSConfig{
			Region:                 "us-west-2",
			UserRegisteredQueueURL: "https://sqs.us-west-2.amazonaws.com/123/queue",
			// Not setting MaxConcurrency, VisibilityTimeout, WaitTimeSeconds
			// Should use defaults from constructor
		},
	}

	consumer, err := NewMessageConsumer(cfg)

	require.NoError(t, err)
	require.NotNil(t, consumer)

	sqsConsumer, ok := consumer.(*aws.Consumer)
	require.True(t, ok)
	assert.NotNil(t, sqsConsumer)
}
