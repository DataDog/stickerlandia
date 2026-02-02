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
import {
  AWSMessagingProps,
  KafkaMessagingProps,
  ServiceProps,
} from "./service-props";

export enum MessagingType {
  AWS,
  KAFKA,
}

export class StickerAwardServiceStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    const serviceName = "sticker-award";
    const environment = process.env.ENV || "dev";
    const messagingType: MessagingType = MessagingType.AWS;

    const sharedResources = new SharedResources(this, "SharedResources", {
      serviceName: serviceName,
      environment: environment,
    });

    const ddSite = process.env.DD_SITE || "datadoghq.com";
    const ddApiKey = process.env.DD_API_KEY || "";

    const ddApiKeyParam = new StringParameter(this, "DDApiKeyParam", {
      parameterName: `/stickerlandia/${environment}/sticker-award/dd-api-key`,
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
      "awards",
      serviceName,
      cluster,
      ddApiKey,
      ddApiKeyParam,
      ddSite,
    );

    // Create formatted database credentials from the shared RDS secret
    const dbCredentials = new DatabaseCredentials(this, "DatabaseCredentials", {
      databaseSecretArn: sharedResources.sharedDatabaseSecretArn,
      sharedProps: sharedProps,
      format: ConnectionStringFormat.POSTGRES_URL,
      databaseName: "stickerlandia_awards",
      vpc: sharedResources.vpc,
    });

    const serviceProps: ServiceProps = {
      cloudfrontDistribution: sharedResources.cloudfrontDistribution,
      cloudfrontEndpoint: sharedResources.cloudfrontEndpoint,
      connectionStringSecret: dbCredentials.connectionStringSecret!,
      databaseCredentials: dbCredentials,
      messagingConfiguration: new AWSMessagingProps(
        this,
        "MessagingProps",
        sharedResources,
      ),
      serviceDependencies: [dbCredentials.credentialResource],
    };

    const api = new Api(this, "Api", {
      sharedProps,
      serviceProps,
      vpc: sharedResources.vpc,
      vpcLink: sharedResources.vpcLink,
      vpcLinkSecurityGroupId: sharedResources.vpcLinkSecurityGroupId,
      httpApi: sharedResources.httpApi,
      serviceDiscoveryName: "awards.api",
      serviceDiscoveryNamespace: sharedResources.serviceDiscoveryNamespace,
      cluster: cluster,
      deployInPrivateSubnet: true,
      sharedEventBus: sharedResources.sharedEventBus,
    });

    // CDK Outputs
    new cdk.CfnOutput(this, "ServiceApiUrl", {
      value: `https://${sharedResources.cloudfrontDistribution.distributionDomainName}/api/awards/v1`,
      description: "Sticker Award Service API URL",
    });
  }
}
