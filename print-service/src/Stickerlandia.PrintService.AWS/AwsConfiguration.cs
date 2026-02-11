/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

// The SQS queue URL is required as a string
#pragma warning disable CA1056

namespace Stickerlandia.PrintService.AWS;

public class AwsConfiguration
{
    public string EventBusName { get; set; } = string.Empty;

    public string UserRegisteredTopicArn { get; set; } = string.Empty;

    public string PrinterTableName { get; set; } = string.Empty;

    public string PrintJobTableName { get; set; } = string.Empty;
}