/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Aspire.Hosting.AWS.DynamoDB;

namespace Stickerlandia.PrintService.Aspire;

internal sealed record InfrastructureResources(
    IResourceBuilder<DynamoDBLocalResource>? DynamoDbResource = null,
    IResourceBuilder<PostgresDatabaseResource>? PostgresResource = null,
    IResourceBuilder<KafkaServerResource>? KafkaResource = null,
    IResourceBuilder<ProjectResource>? MigrationResource = null)
{
    // Legacy property for backwards compatibility
    public IResourceBuilder<DynamoDBLocalResource> DatabaseResource =>
        DynamoDbResource ?? throw new InvalidOperationException("DynamoDB resource is not configured");
}