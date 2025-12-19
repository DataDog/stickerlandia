/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import { Secret } from "aws-cdk-lib/aws-ecs";
import { IEventBus } from "aws-cdk-lib/aws-events";
import { IGrantable } from "aws-cdk-lib/aws-iam";
import { Construct } from "constructs";
import { SharedResources } from "./shared-resources";

/**
 * Interface for messaging configuration that can be applied to ECS services.
 *
 * Implementations provide secrets, environment variables, and IAM permissions
 * needed for a service to communicate via the configured messaging system.
 */
export interface MessagingProps {
  /** Returns secrets to inject into the container (e.g., credentials) */
  asSecrets(): { [key: string]: Secret };

  /** Returns environment variables to set on the container */
  asEnvironmentVariables(): { [key: string]: string };

  /** Grants necessary permissions to the given IAM principal */
  grantPermissions(grantable: IGrantable): void;
}

/**
 * AWS EventBridge messaging configuration.
 *
 * This is the standard implementation for services using AWS-native messaging.
 * It configures the shared EventBridge event bus with consistent environment
 * variable naming:
 *
 * - MESSAGING_PROVIDER: "aws"
 * - EVENT_BUS_NAME: the event bus name
 *
 * Services requiring additional/alternative env var names (e.g., for framework
 * compatibility) should extend this class.
 */
export class AWSMessagingProps extends Construct implements MessagingProps {
  protected readonly sharedEventBus: IEventBus;

  constructor(scope: Construct, id: string, props: SharedResources) {
    super(scope, id);
    this.sharedEventBus = props.sharedEventBus;
  }

  public asSecrets(): { [key: string]: Secret } {
    return {};
  }

  public asEnvironmentVariables(): { [key: string]: string } {
    return {
      MESSAGING_PROVIDER: "aws",
      EVENT_BUS_NAME: this.sharedEventBus.eventBusName,
    };
  }

  public grantPermissions(grantable: IGrantable): void {
    this.sharedEventBus.grantPutEventsTo(grantable);
  }
}
