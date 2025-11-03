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

    const sharedProps = new SharedProps(
      this,
      "catalogue",
      serviceName,
      cluster,
      ddApiKey,
      ddApiKeyParam,
      ddSite
    );

    const serviceProps = {
      jdbcUrl: process.env.JDBC_URL || "",
      dbUsername: process.env.DB_USERNAME || "",
      dbPassword: process.env.DB_PASSWORD || "",
      kafkaBootstrapServers: process.env.KAFKA_BOOTSTRAP_SERVERS || "",
      kafkaUsername: process.env.KAFKA_USERNAME || "",
      kafkaPassword: process.env.KAFKA_PASSWORD || "",
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
      applicationLoadBalancer: sharedResources.applicationLoadBalancer,
      applicationListener: sharedResources.applicationListener,
      stickerImagesBucket: stickerImageBucket,
    });
  }
}
