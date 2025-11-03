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
import {
  IApplicationListener,
  IApplicationLoadBalancer,
} from "aws-cdk-lib/aws-elasticloadbalancingv2";
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
  applicationLoadBalancer: IApplicationLoadBalancer;
  applicationListener: IApplicationListener;
}

export class Api extends Construct {
  constructor(scope: Construct, id: string, props: ApiProps) {
    super(scope, id);
    const webService = new WebService(this, "StickerAwardWebService", {
      vpc: props.vpc,
      vpcLink: props.vpcLink,
      vpcLinkSecurityGroupId: props.vpcLinkSecurityGroupId,
      httpApi: props.httpApi,
      cluster: props.cluster,
      serviceName: props.sharedProps.serviceName,
      environment: props.sharedProps.environment,
      image: "ghcr.io/datadog/stickerlandia/sticker-award-service",
      imageTag: props.sharedProps.version,
      ddApiKey: props.sharedProps.datadog.apiKeyParameter,
      port: 8080,
      environmentVariables: {
        KAFKA_USERNAME: props.serviceProps.kafkaUsername,
        DATABASE_HOST: props.serviceProps.databaseHost,
        ECS_ENABLE_CONTAINER_METADATA: "true",
        ENV: "dev",
        KAFKA_SASL_USERNAME: props.serviceProps.kafkaUsername,
        LOG_LEVEL: "info",
        KAFKA_BROKERS: props.serviceProps.kafkaBootstrapServers,
        KAFKA_SECURITY_PROTOCOL: "SASL_SSL",
        DATABASE_NAME: props.serviceProps.databaseName,
        KAFKA_GROUP_ID: "sticker-award-service",
        DATABASE_PORT: props.serviceProps.databasePort,
        KAFKA_SASL_MECHANISM: "PLAIN",
        LOG_FORMAT: "json",
        KAFKA_ENABLE_TLS: "true",
        KAFKA_PASSWORD: props.serviceProps.kafkaPassword,
        CATALOGUE_BASE_URL: "http://sticker-catalogue:8080",
        DATABASE_SSL_MODE: "require",
        KAFKA_SASL_PASSWORD: props.serviceProps.kafkaPassword,
        DATABASE_PASSWORD: props.serviceProps.dbPassword,
        DATABASE_USER: props.serviceProps.dbUsername,
      },
      secrets: {
        DD_API_KEY: Secret.fromSsmParameter(
          props.sharedProps.datadog.apiKeyParameter
        ),
      },
      path: "/api/awards/v1/{proxy+}",
      healthCheckPath: "/api/awards/v1",
      serviceDiscoveryNamespace: props.serviceDiscoveryNamespace,
      serviceDiscoveryName: props.serviceDiscoveryName,
      deployInPrivateSubnet: props.deployInPrivateSubnet,
      applicationLoadBalancer: props.applicationLoadBalancer,
      applicationListener: props.applicationListener,
    });
  }
}
