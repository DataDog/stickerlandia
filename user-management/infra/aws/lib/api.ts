/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

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
      ddApiKey: props.sharedProps.datadog.apiKeyParameter,
      port: 8080,
      environmentVariables: {
        ConnectionStrings__messaging: "",
        Aws__UserRegisteredTopicArn: this.userRegisteredTopic.topicArn,
        Aws__StickerClaimedQueueUrl: this.stickerClaimedQueue.queueUrl,
        Aws__StickerClaimedDLQUrl: this.stickerClaimedDLQ.queueUrl,
        DEPLOYMENT_HOST_URL: "https://d3nwzze63j01g4.cloudfront.net",
        DRIVING: "ASPNET",
        DRIVEN: "AWS",
        DISABLE_SSL: "true",
      },
      secrets: {
        DD_API_KEY: Secret.fromSsmParameter(
          props.sharedProps.datadog.apiKeyParameter
        ),
        ConnectionStrings__database: Secret.fromSsmParameter(
          props.serviceProps.connectionString
        ),
      },
      path: "/api/users/{proxy+}",
      additionalPathMappings: [
        "/.well-known/{proxy+}",
        "/auth/{proxy+}",
        "/Auth/{proxy+}",
      ],
      healthCheckPath: "/api/users/v1/health",
      serviceDiscoveryNamespace: props.serviceDiscoveryNamespace,
      serviceDiscoveryName: props.serviceDiscoveryName,
      deployInPrivateSubnet: props.deployInPrivateSubnet,
    });

    this.userRegisteredTopic.grantPublish(webService.taskRole);
    this.stickerClaimedQueue.grantSendMessages(webService.taskRole);
    this.stickerClaimedDLQ.grantSendMessages(webService.taskRole);
    this.stickerClaimedQueue.grantConsumeMessages(webService.taskRole);
    this.stickerClaimedDLQ.grantConsumeMessages(webService.taskRole);
  }
}
