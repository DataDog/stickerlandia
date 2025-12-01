/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import * as cdk from "aws-cdk-lib";
import { Construct } from "constructs";
import { DatadogECSFargate, DatadogLambda } from "datadog-cdk-constructs-v2";
import { SharedResources } from "../../../../shared/lib/shared-constructs/lib/shared-resources";
import { Api } from "./api";
import { Cluster } from "aws-cdk-lib/aws-ecs";
import { StringParameter } from "aws-cdk-lib/aws-ssm";
import { SharedProps } from "../../../../shared/lib/shared-constructs/lib/shared-props";
import { Bucket } from "aws-cdk-lib/aws-s3";

export enum MessagingType {
  AWS,
  KAFKA,
}

export class StickerCatalogueServiceStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    const serviceName = "sticker-service";
    const environment = process.env.ENV || "dev";
    const messagingType: MessagingType = MessagingType.AWS;

    const sharedResources = new SharedResources(this, "SharedResources", {
      networkName: `${serviceName}-${environment}-vpc`,
      environment: environment,
    });

    const ddSite = process.env.DD_SITE || "datadoghq.com";
    const ddApiKey = process.env.DD_API_KEY || "";

    const ddApiKeyParam = new StringParameter(this, "DDApiKeyParam", {
      parameterName: `/stickerlandia/${environment}/catalogue/dd-api-key`,
      stringValue: ddApiKey,
      simpleName: false,
    });

    const cluster = new Cluster(this, "ApiCluster", {
      vpc: sharedResources.vpc,
      clusterName: `${serviceName}-${environment}`,
    });
    cluster.enableFargateCapacityProviders();

    const sharedProps = new SharedProps(
      this,
      "catalogue",
      serviceName,
      cluster,
      ddApiKey,
      ddApiKeyParam,
      ddSite,
      false
    );

    const serviceProps = {
      cloudfrontDistribution: sharedResources.cloudfrontDistribution,
      jdbcUrl: StringParameter.fromStringParameterName(
        this,
        "DatabaseHostParam",
        `/stickerlandia/${environment}/catalogue/database-host`
      ),
      databasePort: process.env.DATABASE_PORT || "5432",
      dbUsername: StringParameter.fromStringParameterName(
        this,
        "DatabaseUsernameParam",
        `/stickerlandia/${environment}/catalogue/database-user`
      ),
      dbPassword: StringParameter.fromStringParameterName(
        this,
        "DatabasePasswordParam",
        `/stickerlandia/${environment}/catalogue/database-password`
      ),
      kafkaBootstrapServers:
        messagingType.toString() === "KAFKA"
          ? StringParameter.fromStringParameterName(
              this,
              "KafkaBootstrapServersParam",
              `/stickerlandia/${environment}/sticker-award/kafka-broker`
            )
          : undefined,
      kafkaUsername:
        messagingType.toString() === "KAFKA"
          ? StringParameter.fromStringParameterName(
              this,
              "KafkaUsernameParam",
              `/stickerlandia/${environment}/sticker-award/kafka-username`
            )
          : undefined,
      kafkaPassword:
        messagingType.toString() === "KAFKA"
          ? StringParameter.fromStringParameterName(
              this,
              "KafkaPasswordParam",
              `/stickerlandia/${environment}/sticker-award/kafka-password`
            )
          : undefined,
      jaslConfig:
        messagingType.toString() === "KAFKA"
          ? StringParameter.fromStringParameterName(
              this,
              "KafkaJaslConfigParam",
              `/stickerlandia/${environment}/sticker-award/kafka-jasl-config`
            )
          : undefined,
    };

    const stickerImageBucket = new Bucket(this, "StickerImageBucket", {
      bucketName: `sticker-images-${environment}-${cdk.Stack.of(this).account}`,
      removalPolicy: cdk.RemovalPolicy.DESTROY,
      autoDeleteObjects: true,
    });

    const api = new Api(this, "Api", {
      messagingType,
      sharedProps: sharedProps,
      serviceProps: serviceProps,
      vpc: sharedResources.vpc,
      vpcLink: sharedResources.vpcLink,
      vpcLinkSecurityGroupId: sharedResources.vpcLinkSecurityGroupId,
      httpApi: sharedResources.httpApi,
      serviceDiscoveryName: "catalogue.api",
      serviceDiscoveryNamespace: sharedResources.serviceDiscoveryNamespace,
      cluster: cluster,
      stickerImagesBucket: stickerImageBucket,
      deployInPrivateSubnet: true,
      sharedEventBus: sharedResources.sharedEventBus,
    });
  }
}
