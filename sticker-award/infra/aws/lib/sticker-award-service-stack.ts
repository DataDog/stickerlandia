/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import * as cdk from "aws-cdk-lib";
import { Construct } from "constructs";
import { SharedResources } from "../../../../shared/lib/shared-constructs/lib/shared-resources";
import { Api } from "./api";
import { Cluster } from "aws-cdk-lib/aws-ecs";
import { StringParameter } from "aws-cdk-lib/aws-ssm";
import { SharedProps } from "../../../../shared/lib/shared-constructs/lib/shared-props";
import { KafkaMessagingProps, ServiceProps } from "./service-props";

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
      networkName: `${serviceName}-${environment}-vpc`,
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
      ddSite
    );

    const serviceProps: ServiceProps = {
      cloudfrontDistribution: sharedResources.cloudfrontDistribution,
      databaseHost: StringParameter.fromStringParameterName(
        this,
        "DatabaseHostParam",
        `/stickerlandia/${environment}/sticker-award/database-host`
      ),
      databaseName: StringParameter.fromStringParameterName(
        this,
        "DatabaseNameParam",
        `/stickerlandia/${environment}/sticker-award/database-name`
      ),
      databasePort: process.env.DATABASE_PORT || "5432",
      dbUsername: StringParameter.fromStringParameterName(
        this,
        "DatabaseUsernameParam",
        `/stickerlandia/${environment}/sticker-award/database-user`
      ),
      dbPassword: StringParameter.fromStringParameterName(
        this,
        "DatabasePasswordParam",
        `/stickerlandia/${environment}/sticker-award/database-password`
      ),
      messagingConfiguration: new KafkaMessagingProps(
        this,
        "KafkaMessagingProps",
        sharedProps
      ),
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
  }
}
