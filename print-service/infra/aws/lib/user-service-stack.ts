/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import * as cdk from "aws-cdk-lib";
import { Construct } from "constructs";
import { SharedProps } from "../../../../shared/lib/shared-constructs/lib/shared-props";
import { SharedResources } from "../../../../shared/lib/shared-constructs/lib/shared-resources";
import {
  DatabaseCredentials,
  ConnectionStringFormat,
} from "../../../../shared/lib/shared-constructs/lib/database-credentials";
import { MigrationTask } from "../../../../shared/lib/shared-constructs/lib/migration-task";
import { Api } from "./api";
import { Cluster, Secret } from "aws-cdk-lib/aws-ecs";
import * as path from "path";
import { BackgroundWorkers } from "./background-workers";
import { StringParameter } from "aws-cdk-lib/aws-ssm";
import {
  AWSMessagingProps,
  ServiceProps,
} from "./service-props";

export class UserServiceStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    const serviceName = "user-service";
    const environment = process.env.ENV || "dev";

    const sharedResources = new SharedResources(this, "SharedResources", {
      networkName: `${serviceName}-${environment}-vpc`,
      environment: environment,
    });

    const ddSite = process.env.DD_SITE || "datadoghq.com";
    const ddApiKey = process.env.DD_API_KEY || "";

    const ddApiKeyParam = new StringParameter(this, "DDApiKeyParam", {
      parameterName: `/stickerlandia/${environment}/users/dd-api-key`,
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
      "users",
      serviceName,
      cluster,
      ddApiKey,
      ddApiKeyParam,
      ddSite
    );

    // Create formatted database credentials from the shared RDS secret
    const dbCredentials = new DatabaseCredentials(this, "DatabaseCredentials", {
      databaseSecretArn: sharedResources.sharedDatabaseSecretArn,
      environment: environment,
      serviceName: "users",
      format: ConnectionStringFormat.DOTNET,
      // Don't create SSM parameter references - they cause CloudFormation validation errors.
      // Lambda functions will use CloudFormation dynamic references instead.
      createSsmParameterReferences: false,
    });

    // Run database migrations before starting services
    const imageTag = process.env.VERSION || "LOCAL";
    const migrationTask = new MigrationTask(this, "MigrationTask", {
      sharedProps: sharedProps,
      vpc: sharedResources.vpc,
      cluster: cluster,
      image: "ghcr.io/datadog/stickerlandia/user-management-migration",
      imageTag: imageTag,
      assetPath: path.join(__dirname, "../../../"),
      entryPoint: ["dotnet"],
      command: ["migrations/Stickerlandia.PrintService.MigrationService.dll"],
      environmentVariables: {
        DEPLOYMENT_HOST_URL: `https://${sharedResources.cloudfrontDistribution.distributionDomainName}`,
        DRIVING: "ASPNET",
        DRIVEN: "AWS",
        DISABLE_SSL: "true",
      },
      secrets: {
        "ConnectionStrings__database": dbCredentials.getConnectionStringEcsSecret()!,
      },
      // Use the same security group as the API service for consistent network access
      securityGroupId: sharedResources.vpcLinkSecurityGroupId,
      dependencies: [dbCredentials.credentialResource],
      deployInPrivateSubnet: true,
      timeout: 300,
    });

    // Grant migration task permission to read database credentials
    dbCredentials.grantRead(migrationTask.executionRole);

    const serviceProps: ServiceProps = {
      cloudfrontDistribution: sharedResources.cloudfrontDistribution,
      connectionStringSecret: dbCredentials.connectionStringSecret!,
      connectionStringParameter: dbCredentials.connectionStringParameter,
      databaseCredentials: dbCredentials,
      messagingConfiguration: new AWSMessagingProps(
        this,
        "MessagingProps",
        sharedResources
      ),
      // Services depend on both DB credentials and migration completing
      serviceDependencies: [dbCredentials.credentialResource, migrationTask.resource],
    };

    const api = new Api(this, "Api", {
      sharedProps: sharedProps,
      serviceProps,
      vpc: sharedResources.vpc,
      vpcLink: sharedResources.vpcLink,
      vpcLinkSecurityGroupId: sharedResources.vpcLinkSecurityGroupId,
      httpApi: sharedResources.httpApi,
      serviceDiscoveryName: "users.api",
      serviceDiscoveryNamespace: sharedResources.serviceDiscoveryNamespace,
      cluster: cluster,
      deployInPrivateSubnet: true,
    });

    const backgroundWorkers = new BackgroundWorkers(this, "BackgroundWorkers", {
      sharedProps: sharedProps,
      serviceProps,
      sharedEventBus: sharedResources.sharedEventBus,
      vpc: sharedResources.vpc,
      vpcLinkSecurityGroupId: sharedResources.vpcLinkSecurityGroupId,
      serviceDiscoveryName: "users.worker",
      cluster: cluster,
      useLambda: true,
      serviceDiscoveryNamespace: sharedResources.serviceDiscoveryNamespace,
      stickerClaimedQueue: api.stickerClaimedQueue,
      stickerClaimedDLQ: api.stickerClaimedDLQ,
      userRegisteredTopic: api.userRegisteredTopic,
      deployInPrivateSubnet: true,
    });

    // CDK Outputs
    new cdk.CfnOutput(this, "ServiceApiUrl", {
      value: `https://${sharedResources.cloudfrontDistribution.distributionDomainName}/api/users/v1`,
      description: "User Management Service API URL",
    });
  }
}
