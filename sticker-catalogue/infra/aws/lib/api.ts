/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import { IVpc } from "aws-cdk-lib/aws-ec2";
import { Construct } from "constructs";
import { Cluster, Secret } from "aws-cdk-lib/aws-ecs";
import { Topic } from "aws-cdk-lib/aws-sns";
import { Queue } from "aws-cdk-lib/aws-sqs";
import { IHttpApi, IVpcLink } from "aws-cdk-lib/aws-apigatewayv2";
import { IPrivateDnsNamespace } from "aws-cdk-lib/aws-servicediscovery";
import { SharedProps } from "../../../../shared/lib/shared-constructs/lib/shared-props";
import { WebService } from "../../../../shared/lib/shared-constructs/lib/web-service";
import { Bucket } from "aws-cdk-lib/aws-s3";
import { ServiceProps } from "./service-props";
import { IEventBus } from "aws-cdk-lib/aws-events";

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
  stickerImagesBucket: Bucket;
  sharedEventBus: IEventBus;
}

export class Api extends Construct {
  stickerClaimedQueue: Queue;
  stickerClaimedDLQ: Queue;
  userRegisteredTopic: Topic;
  constructor(scope: Construct, id: string, props: ApiProps) {
    super(scope, id);

    const secrets: { [key: string]: Secret } = {
      DD_API_KEY: Secret.fromSsmParameter(
        props.sharedProps.datadog.apiKeyParameter
      ),
      QUARKUS_DATASOURCE_JDBC_URL: Secret.fromSsmParameter(
        props.serviceProps.jdbcUrl
      ),
      QUARKUS_DATASOURCE_USERNAME: Secret.fromSsmParameter(
        props.serviceProps.dbUsername
      ),
      QUARKUS_DATASOURCE_PASSWORD: Secret.fromSsmParameter(
        props.serviceProps.dbPassword
      ),
      ...props.serviceProps.messagingProps.asSecrets(),
    };

    const webService = new WebService(this, "StickerCatalogueWebService", {
      sharedProps: props.sharedProps,
      vpc: props.vpc,
      vpcLink: props.vpcLink,
      vpcLinkSecurityGroupId: props.vpcLinkSecurityGroupId,
      httpApi: props.httpApi,
      cluster: props.cluster,
      image: "ghcr.io/datadog/stickerlandia/sticker-catalogue-service",
      imageTag: props.sharedProps.version,
      ddApiKey: props.sharedProps.datadog.apiKeyParameter,
      port: 8080,
      environmentVariables: {
        QUARKUS_DATASOURCE_DB_KIND: "postgresql",
        QUARKUS_DATASOURCE_DEVSERVICES_ENABLED: "false",
        QUARKUS_DATASOURCE_JDBC_ACQUISITION_TIMEOUT: "30S",
        QUARKUS_S3_PATH_STYLE_ACCESS: "true",
        STICKER_IMAGES_BUCKET: props.stickerImagesBucket.bucketName,
        ...props.serviceProps.messagingProps.asEnvironmentVariables(),
      },
      secrets: secrets,
      path: "/api/stickers/v1/{proxy+}",
      additionalPathMappings: [],
      healthCheckPath: "/api/stickers/v1",
      serviceDiscoveryNamespace: props.serviceDiscoveryNamespace,
      serviceDiscoveryName: props.serviceDiscoveryName,
      deployInPrivateSubnet: props.deployInPrivateSubnet,
    });

    props.stickerImagesBucket.grantReadWrite(webService.taskRole);
    props.serviceProps.messagingProps.grantPermissions(webService.taskRole);
  }
}
