/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import { IVpc } from "aws-cdk-lib/aws-ec2";
import { Construct } from "constructs";
import { Cluster, Secret } from "aws-cdk-lib/aws-ecs";
import { IHttpApi, IVpcLink } from "aws-cdk-lib/aws-apigatewayv2";
import { IPrivateDnsNamespace } from "aws-cdk-lib/aws-servicediscovery";
import { SharedProps } from "../../../../shared/lib/shared-constructs/lib/shared-props";
import { WebService } from "../../../../shared/lib/shared-constructs/lib/web-service";
import { ServiceProps } from "./service-props";
import { IEventBus, Rule } from "aws-cdk-lib/aws-events";
import { Queue } from "aws-cdk-lib/aws-sqs";
import { Duration } from "aws-cdk-lib";
import { MessagingType } from "./sticker-award-service-stack";
import { SqsQueue } from "aws-cdk-lib/aws-events-targets";

export class ApiProps {
  sharedProps: SharedProps;
  serviceProps: ServiceProps;
  sharedEventBus: IEventBus;
  vpc: IVpc;
  vpcLink: IVpcLink;
  vpcLinkSecurityGroupId: string;
  httpApi: IHttpApi;
  serviceDiscoveryNamespace: IPrivateDnsNamespace;
  serviceDiscoveryName: string;
  deployInPrivateSubnet?: boolean;
  cluster: Cluster;
  messagingType: MessagingType;
}

export class Api extends Construct {
  constructor(scope: Construct, id: string, props: ApiProps) {
    super(scope, id);

    const userRegisteredDLQ = new Queue(this, "UserRegisteredDLQ", {
      queueName: `stickers-user-registered-dlq-${props.sharedProps.environment}`,
      visibilityTimeout: Duration.seconds(120),
    });

    const userRegisteredQueue = new Queue(this, "UserRegisteredQueue", {
      queueName: `stickers-user-registered-${props.sharedProps.environment}`,
      visibilityTimeout: Duration.seconds(30),
      deadLetterQueue: {
        maxReceiveCount: 3,
        queue: userRegisteredDLQ,
      },
    });

    const rule = new Rule(this, "UserRegisteredEventRule", {
      eventBus: props.sharedEventBus,
      ruleName: `${props.sharedProps.serviceName}-${props.sharedProps.environment}-user-registered-rule`,
      eventPattern: {
        source: [`${props.sharedProps.environment}.users`],
        detailType: ["users.userRegistered.v1"],
      },
    });
    rule.addTarget(new SqsQueue(userRegisteredQueue));

    const secrets: { [key: string]: Secret } = {
      DD_API_KEY: Secret.fromSsmParameter(
        props.sharedProps.datadog.apiKeyParameter
      ),
      DATABASE_HOST: Secret.fromSsmParameter(props.serviceProps.databaseHost),
      DATABASE_NAME: Secret.fromSsmParameter(props.serviceProps.databaseName),
      DATABASE_PASSWORD: Secret.fromSsmParameter(props.serviceProps.dbPassword),
      DATABASE_USER: Secret.fromSsmParameter(props.serviceProps.dbUsername),
    };

    if (props.messagingType.toString() === "KAFKA") {
      secrets.KAFKA_USERNAME = Secret.fromSsmParameter(
        props.serviceProps.kafkaUsername!
      );
      secrets.KAFKA_SASL_USERNAME = Secret.fromSsmParameter(
        props.serviceProps.kafkaUsername!
      );
      secrets.KAFKA_BROKERS = Secret.fromSsmParameter(
        props.serviceProps.kafkaBootstrapServers!
      );
      secrets.KAFKA_PASSWORD = Secret.fromSsmParameter(
        props.serviceProps.kafkaPassword!
      );
      secrets.KAFKA_SASL_PASSWORD = Secret.fromSsmParameter(
        props.serviceProps.kafkaPassword!
      );
    }

    const webService = new WebService(this, "StickerAwardWebService", {
      sharedProps: props.sharedProps,
      vpc: props.vpc,
      vpcLink: props.vpcLink,
      vpcLinkSecurityGroupId: props.vpcLinkSecurityGroupId,
      httpApi: props.httpApi,
      cluster: props.cluster,
      image: "ghcr.io/datadog/stickerlandia/sticker-award-service",
      imageTag: props.sharedProps.version,
      ddApiKey: props.sharedProps.datadog.apiKeyParameter,
      port: 8080,
      environmentVariables: {
        ENV: "dev",
        LOG_LEVEL: "info",
        KAFKA_SECURITY_PROTOCOL: "SASL_SSL",
        KAFKA_GROUP_ID: "sticker-award-service",
        DATABASE_PORT: props.serviceProps.databasePort,
        KAFKA_SASL_MECHANISM: "PLAIN",
        LOG_FORMAT: "json",
        KAFKA_ENABLE_TLS: "true",
        CATALOGUE_BASE_URL: `https://${props.serviceProps.cloudfrontDistribution.distributionDomainName}`,
        DATABASE_SSL_MODE: "require",
        USER_REGISTERED_QUEUE_URL: userRegisteredQueue.queueUrl,
      },
      secrets: secrets,
      path: "/api/awards/v1/{proxy+}",
      healthCheckPath: "/api/awards/v1",
      serviceDiscoveryNamespace: props.serviceDiscoveryNamespace,
      serviceDiscoveryName: props.serviceDiscoveryName,
      deployInPrivateSubnet: props.deployInPrivateSubnet,
      additionalPathMappings: [],
    });

    props.sharedEventBus.grantPutEventsTo(webService.taskRole);
    userRegisteredQueue.grantConsumeMessages(webService.taskRole);
  }
}
