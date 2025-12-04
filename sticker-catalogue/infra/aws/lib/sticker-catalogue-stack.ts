/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import * as cdk from "aws-cdk-lib";
import { Construct } from "constructs";
import { SharedResources } from "../../../../shared/lib/shared-constructs/lib/shared-resources";
import {
  DatabaseCredentials,
  ConnectionStringFormat,
} from "../../../../shared/lib/shared-constructs/lib/database-credentials";
import { Api } from "./api";
import { Cluster } from "aws-cdk-lib/aws-ecs";
import { StringParameter } from "aws-cdk-lib/aws-ssm";
import { SharedProps } from "../../../../shared/lib/shared-constructs/lib/shared-props";
import { Bucket } from "aws-cdk-lib/aws-s3";
import { KafkaMessagingProps, ServiceProps } from "./service-props";

export enum MessagingType {
  AWS,
  KAFKA,
}

export class StickerCatalogueServiceStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    const serviceName = "sticker-service";
    const environment = process.env.ENV || "dev";

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

    // Create formatted database credentials from the shared RDS secret
    const dbCredentials = new DatabaseCredentials(this, "DatabaseCredentials", {
      databaseSecretArn: sharedResources.sharedDatabaseSecretArn,
      environment: environment,
      serviceName: "catalogue",
      format: ConnectionStringFormat.INDIVIDUAL_FIELDS,
    });

    const serviceProps: ServiceProps = {
      cloudfrontDistribution: sharedResources.cloudfrontDistribution,
      jdbcUrl: dbCredentials.jdbcUrlParameter!,
      dbUsername: dbCredentials.usernameParameter!,
      dbPassword: dbCredentials.passwordParameter!,
      messagingProps: new KafkaMessagingProps(this, "MessagingProps", sharedProps),
    };

    const stickerImageBucket = new Bucket(this, "StickerImageBucket", {
      bucketName: `sticker-images-${environment}-${cdk.Stack.of(this).account}`,
      removalPolicy: cdk.RemovalPolicy.DESTROY,
      autoDeleteObjects: true,
    });

    const api = new Api(this, "Api", {
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
