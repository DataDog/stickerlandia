/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import * as path from "path";
import { IVpc } from "aws-cdk-lib/aws-ec2";
import { Construct } from "constructs";
import { SharedProps } from "../../../../shared/lib/shared-constructs/lib/shared-props";
import { Cluster, Secret } from "aws-cdk-lib/aws-ecs";
import { Topic } from "aws-cdk-lib/aws-sns";
import { Queue } from "aws-cdk-lib/aws-sqs";
import { IHttpApi, IVpcLink } from "aws-cdk-lib/aws-apigatewayv2";
import { IPrivateDnsNamespace } from "aws-cdk-lib/aws-servicediscovery";
import { WebService } from "../../../../shared/lib/shared-constructs/lib/web-service";
import { ServiceProps } from "./service-props";
import { Duration } from "aws-cdk-lib/core";

export class ApiProps {
  sharedProps: SharedProps;
  serviceProps: ServiceProps;
  vpc: IVpc;
  vpcLink: IVpcLink;
  vpcLinkSecurityGroupId: string;
  httpApi: IHttpApi;
  serviceDiscoveryNamespace: IPrivateDnsNamespace;
  serviceDiscoveryName: string;
  deployInPrivateSubnet?: boolean;
  cluster: Cluster;
}

export class Api extends Construct {
  stickerClaimedQueue: Queue;
  stickerClaimedDLQ: Queue;
  userRegisteredTopic: Topic;
  constructor(scope: Construct, id: string, props: ApiProps) {
    super(scope, id);

    this.userRegisteredTopic = new Topic(this, "UserRegisteredTopic", {
      topicName: `${props.sharedProps.serviceName}-${props.sharedProps.environment}-user-registered`,
    });
    this.stickerClaimedDLQ = new Queue(this, "StickerClaimedDLQ", {
      queueName: `${props.sharedProps.serviceName}-${props.sharedProps.environment}-sticker-claimed-dlq`,
    });

    //TODO: Add EventBridge rule mapping to subscribe to sticker claimed events published to the shared EventBus.
    this.stickerClaimedQueue = new Queue(this, "StickerClaimedQueue", {
      queueName: `${props.sharedProps.serviceName}-${props.sharedProps.environment}-sticker-claimed`,
      deadLetterQueue: {
        queue: this.stickerClaimedDLQ,
        maxReceiveCount: 5, // Messages will be sent to DLQ after 5 failed attempts
      },
    });

    const webService = new WebService(this, "UserServiceWebService", {
      sharedProps: props.sharedProps,
      vpc: props.vpc,
      vpcLink: props.vpcLink,
      vpcLinkSecurityGroupId: props.vpcLinkSecurityGroupId,
      httpApi: props.httpApi,
      cluster: props.cluster,
      image: "ghcr.io/datadog/stickerlandia/user-management-service",
      imageTag: props.sharedProps.version,
      assetPath: path.resolve(__dirname, "../../.."),
      dockerfile: "src/Stickerlandia.UserManagement.Api/Dockerfile",
      ddApiKey: props.sharedProps.datadog.apiKeyParameter,
      port: 8080,
      environmentVariables: {
        DD_TRACE_OTEL_ENABLED: "true",
        DD_LOGS_INJECTION: "true",
        DD_RUNTIME_METRICS_ENABLED: "true",
        DD_PROFILING_ENABLED: "true",
        DEPLOYMENT_HOST_URL: `https://${props.serviceProps.cloudfrontDistribution.distributionDomainName}`,
        DRIVING: "ASPNET",
        DRIVEN: "AWS",
        DISABLE_SSL: "true",
        LOGGING__LOGLEVEL__DEFAULT: "INFORMATION",
        LOGGING__LOGLEVEL__MICROSOFT: "INFORMATION",
        "LOGGING__LOGLEVEL__MICROSOFT.ENTITYFRAMEWORKCORE.DATABASE.COMMAND":
          "WARNING",
        ...props.serviceProps.messagingConfiguration.asEnvironmentVariables(),
      },
      secrets: {
        DD_API_KEY: Secret.fromSsmParameter(
          props.sharedProps.datadog.apiKeyParameter
        ),
        ConnectionStrings__database:
          props.serviceProps.databaseCredentials.getConnectionStringEcsSecret()!,
        ...props.serviceProps.messagingConfiguration.asSecrets(),
      },
      path: "/api/users/{proxy+}",
      additionalPathMappings: [
        "/.well-known/{proxy+}",
        "/auth/{proxy+}",
        "/Auth/{proxy+}",
      ],
      healthCheckPath: "/api/users/v1/health",
      healthCheckCommand: {
        command: [
          "CMD-SHELL",
          `curl -f http://localhost:8080/api/users/v1/health || exit 1`,
        ],
        interval: Duration.seconds(30),
        timeout: Duration.seconds(5),
        retries: 3,
        startPeriod: Duration.seconds(60),
      },
      serviceDiscoveryNamespace: props.serviceDiscoveryNamespace,
      serviceDiscoveryName: props.serviceDiscoveryName,
      deployInPrivateSubnet: props.deployInPrivateSubnet,
      serviceDependencies: props.serviceProps.serviceDependencies,
    });

    this.userRegisteredTopic.grantPublish(webService.taskRole);
    this.stickerClaimedQueue.grantSendMessages(webService.taskRole);
    this.stickerClaimedDLQ.grantSendMessages(webService.taskRole);
    this.stickerClaimedQueue.grantConsumeMessages(webService.taskRole);
    this.stickerClaimedDLQ.grantConsumeMessages(webService.taskRole);

    // Grant execution role permission to read the database connection string secret
    props.serviceProps.databaseCredentials.grantRead(webService.executionRole);
  }
}
