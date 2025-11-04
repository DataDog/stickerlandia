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
  constructor(scope: Construct, id: string, props: ApiProps) {
    super(scope, id);
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
        ECS_ENABLE_CONTAINER_METADATA: "true",
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
      },
      secrets: {
        DD_API_KEY: Secret.fromSsmParameter(
          props.sharedProps.datadog.apiKeyParameter
        ),
        KAFKA_USERNAME: Secret.fromSsmParameter(
          props.serviceProps.kafkaUsername
        ),
        DATABASE_HOST: Secret.fromSsmParameter(props.serviceProps.databaseHost),
        KAFKA_SASL_USERNAME: Secret.fromSsmParameter(
          props.serviceProps.kafkaUsername
        ),
        KAFKA_BROKERS: Secret.fromSsmParameter(
          props.serviceProps.kafkaBootstrapServers
        ),
        DATABASE_NAME: Secret.fromSsmParameter(props.serviceProps.databaseName),
        KAFKA_PASSWORD: Secret.fromSsmParameter(
          props.serviceProps.kafkaPassword
        ),
        KAFKA_SASL_PASSWORD: Secret.fromSsmParameter(
          props.serviceProps.kafkaPassword
        ),
        DATABASE_PASSWORD: Secret.fromSsmParameter(
          props.serviceProps.dbPassword
        ),
        DATABASE_USER: Secret.fromSsmParameter(props.serviceProps.dbUsername),
      },
      path: "/api/awards/v1/{proxy+}",
      healthCheckPath: "/api/awards/v1",
      serviceDiscoveryNamespace: props.serviceDiscoveryNamespace,
      serviceDiscoveryName: props.serviceDiscoveryName,
      deployInPrivateSubnet: props.deployInPrivateSubnet,
      additionalPathMappings: [],
    });
  }
}
