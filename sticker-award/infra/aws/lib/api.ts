/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import * as path from "path";
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
      DATABASE_URL: props.serviceProps.databaseCredentials.getConnectionStringEcsSecret()!,
      ...props.serviceProps.messagingConfiguration.asSecrets(),
    };

    const webService = new WebService(this, "StickerAwardWebService", {
      sharedProps: props.sharedProps,
      vpc: props.vpc,
      vpcLink: props.vpcLink,
      vpcLinkSecurityGroupId: props.vpcLinkSecurityGroupId,
      httpApi: props.httpApi,
      cluster: props.cluster,
      image: "ghcr.io/datadog/stickerlandia/sticker-award-service",
      imageTag: props.sharedProps.version,
      assetPath: path.resolve(__dirname, "../../.."),
      ddApiKey: props.sharedProps.datadog.apiKeyParameter,
      port: 8080,
      environmentVariables: {
        LOG_LEVEL: "info",
        LOG_FORMAT: "json",
        MESSAGING_PROVIDER: "aws",
        CATALOGUE_BASE_URL: `https://${props.serviceProps.cloudfrontDistribution.distributionDomainName}`,
        USER_REGISTERED_QUEUE_URL: userRegisteredQueue.queueUrl,
        OAUTH_ISSUER: `https://${props.serviceProps.cloudfrontDistribution.distributionDomainName}`,
        ...props.serviceProps.messagingConfiguration.asEnvironmentVariables(),
      },
      secrets: secrets,
      path: "/api/awards/v1/{proxy+}",
      healthCheckPath: "/api/awards/v1",
      serviceDiscoveryNamespace: props.serviceDiscoveryNamespace,
      serviceDiscoveryName: props.serviceDiscoveryName,
      deployInPrivateSubnet: props.deployInPrivateSubnet,
      additionalPathMappings: [],
      serviceDependencies: props.serviceProps.serviceDependencies,
    });

    props.serviceProps.messagingConfiguration.grantPermissions(
      webService.taskRole
    );
    userRegisteredQueue.grantConsumeMessages(webService.taskRole);

    // Grant execution role permission to read the database connection string secret
    // This is necessary because Secret.fromSecretNameV2() doesn't include the random suffix
    // that Secrets Manager adds to ARNs, so CDK's automatic grants may not work correctly
    props.serviceProps.databaseCredentials.grantRead(webService.executionRole);
  }
}
