/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import * as cdk from "aws-cdk-lib";
import { Construct } from "constructs";
import { SharedProps } from "../../../../shared/lib/shared-constructs/lib/shared-props";
import { SharedResources } from "../../../../shared/lib/shared-constructs/lib/shared-resources";
import { Api } from "./api";
import { Cluster } from "aws-cdk-lib/aws-ecs";
import { StringParameter } from "aws-cdk-lib/aws-ssm";
import { AWSMessagingProps, ServiceProps } from "./service-props";
import { StreamViewType, Table } from "aws-cdk-lib/aws-dynamodb";
import { BackgroundWorkers } from "./background-workers";

export class PrintServiceStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    const serviceName = "print-service";
    const environment = process.env.ENV || "dev";

    const sharedResources = new SharedResources(this, "SharedResources", {
      networkName: `${serviceName}-${environment}-vpc`,
      environment: environment,
    });

    const ddSite = process.env.DD_SITE || "datadoghq.com";
    const ddApiKey = process.env.DD_API_KEY || "";

    const ddApiKeyParam = new StringParameter(this, "DDApiKeyParam", {
      parameterName: `/stickerlandia/${environment}/print/dd-api-key`,
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
      "print",
      serviceName,
      cluster,
      ddApiKey,
      ddApiKeyParam,
      ddSite,
      true,
      {
        DD_APM_IGNORE_RESOURCES:
          "(GET|HEAD) .*/health$,POST localhost:4317/opentelemetry.proto.collector.trace.v1.TraceService/Export,POST localhost:4317/opentelemetry.proto.collector.metrics.v1.MetricsService/Export",
      },
    );

    const serviceProps: ServiceProps = {
      cloudfrontDistribution: sharedResources.cloudfrontDistribution,
      messagingConfiguration: new AWSMessagingProps(
        this,
        "MessagingProps",
        sharedResources
      ),
      serviceDependencies: [],
    };

    // The data model of the Print Service supports the constraints that DynamoDB imposes on an application
    // Using DynamoDB instead of Postgres
    const isProduction = environment === "prod";

    const printerTable = new Table(this, "PrinterTable", {
      tableName: `Printers-${environment}`,
      partitionKey: { name: "PK", type: cdk.aws_dynamodb.AttributeType.STRING },
      sortKey: { name: "SK", type: cdk.aws_dynamodb.AttributeType.STRING },
      removalPolicy: isProduction ? cdk.RemovalPolicy.RETAIN : cdk.RemovalPolicy.DESTROY,
      billingMode: cdk.aws_dynamodb.BillingMode.PAY_PER_REQUEST,
      pointInTimeRecovery: true,
      deletionProtection: isProduction,
      stream: StreamViewType.NEW_IMAGE,
      timeToLiveAttribute: "TTL",
    });
    printerTable.addGlobalSecondaryIndex({
      indexName: "GSI1",
      partitionKey: {
        name: "GSI1PK",
        type: cdk.aws_dynamodb.AttributeType.STRING,
      },
      projectionType: cdk.aws_dynamodb.ProjectionType.ALL,
    });

    const printJobTable = new Table(this, "PrintJobTable", {
      tableName: `PrintJobs-${environment}`,
      partitionKey: {
        name: "PK",
        type: cdk.aws_dynamodb.AttributeType.STRING,
      },
      sortKey: { name: "SK", type: cdk.aws_dynamodb.AttributeType.STRING },
      removalPolicy: isProduction ? cdk.RemovalPolicy.RETAIN : cdk.RemovalPolicy.DESTROY,
      billingMode: cdk.aws_dynamodb.BillingMode.PAY_PER_REQUEST,
      pointInTimeRecovery: true,
      deletionProtection: isProduction,
      stream: StreamViewType.NEW_IMAGE,
      timeToLiveAttribute: "TTL",
    });
    printJobTable.addGlobalSecondaryIndex({
      indexName: "GSI1",
      partitionKey: {
        name: "GSI1PK",
        type: cdk.aws_dynamodb.AttributeType.STRING,
      },
      sortKey: {
        name: "GSI1SK",
        type: cdk.aws_dynamodb.AttributeType.STRING,
      },
      projectionType: cdk.aws_dynamodb.ProjectionType.ALL,
    });

    const api = new Api(this, "Api", {
      sharedProps: sharedProps,
      serviceProps,
      vpc: sharedResources.vpc,
      vpcLink: sharedResources.vpcLink,
      vpcLinkSecurityGroupId: sharedResources.vpcLinkSecurityGroupId,
      httpApi: sharedResources.httpApi,
      serviceDiscoveryName: "print.api",
      serviceDiscoveryNamespace: sharedResources.serviceDiscoveryNamespace,
      cluster: cluster,
      deployInPrivateSubnet: true,
      printerTable: printerTable,
      printJobTable: printJobTable,
    });

    const backgroundWorkers = new BackgroundWorkers(this, "BackgroundWorkers", {
      sharedProps,
      serviceProps,
      sharedEventBus: sharedResources.sharedEventBus,
      printerTable: printerTable,
      printJobTable: printJobTable,
    });

    // CDK Outputs
    new cdk.CfnOutput(this, "ServiceApiUrl", {
      value: `https://${sharedResources.cloudfrontDistribution.distributionDomainName}/api/print/v1`,
      description: "Print Service API URL",
    });
  }
}
