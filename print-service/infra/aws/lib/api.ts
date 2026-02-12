/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import * as path from "path";
import { IVpc } from "aws-cdk-lib/aws-ec2";
import { Construct } from "constructs";
import { SharedProps } from "../../../../shared/lib/shared-constructs/lib/shared-props";
import { Cluster, Secret } from "aws-cdk-lib/aws-ecs";
import { Topic } from "aws-cdk-lib/aws-sns";
import { Queue } from "aws-cdk-lib/aws-sqs";
import { IHttpApi, IVpcLink } from "aws-cdk-lib/aws-apigatewayv2";
import { IPrivateDnsNamespace } from "aws-cdk-lib/aws-servicediscovery";
import { WebService } from "../../../../shared/lib/shared-constructs/lib/web-service";
import { ServiceProps } from "./service-props";
import { Duration } from "aws-cdk-lib/core";
import { ITable } from "aws-cdk-lib/aws-dynamodb";

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
  printerTable: ITable;
  printJobTable: ITable;
}

export class Api extends Construct {
  constructor(scope: Construct, id: string, props: ApiProps) {
    super(scope, id);

    const webService = new WebService(this, "PrintServiceWebService", {
      sharedProps: props.sharedProps,
      vpc: props.vpc,
      vpcLink: props.vpcLink,
      vpcLinkSecurityGroupId: props.vpcLinkSecurityGroupId,
      httpApi: props.httpApi,
      cluster: props.cluster,
      image: "ghcr.io/datadog/stickerlandia/print-service",
      imageTag: props.sharedProps.version,
      assetPath: path.resolve(__dirname, "../../.."),
      dockerfile: "src/Stickerlandia.PrintService.Api/Dockerfile",
      ddApiKey: props.sharedProps.datadog.apiKeyParameter,
      port: 8080,
      memoryLimitMiB: 512,
      environmentVariables: {
        DD_DATA_STREAMS_ENABLED: "true",
        DD_TRACE_OTEL_ENABLED: "true",
        DD_LOGS_INJECTION: "true",
        DD_RUNTIME_METRICS_ENABLED: "true",
        DD_PROFILING_ENABLED: "true",
        DEPLOYMENT_HOST_URL: props.serviceProps.cloudfrontDistribution.distributionDomainName,
        OTEL_EXPORTER_OTLP_ENDPOINT: "http://localhost:4317",
        OTEL_SERVICE_NAME: props.sharedProps.serviceName,
        DRIVING: "AWS",
        DRIVEN: "AWS",
        Aws__PrinterTableName: props.printerTable.tableName,
        Aws__PrintJobTableName: props.printJobTable.tableName,
        DISABLE_SSL: "true",
        LOGGING__LOGLEVEL__DEFAULT: "INFORMATION",
        LOGGING__LOGLEVEL__MICROSOFT: "INFORMATION",
        "LOGGING__LOGLEVEL__MICROSOFT.ENTITYFRAMEWORKCORE.DATABASE.COMMAND":
          "WARNING",
        Authentication__Audience: "stickerlandia",
        Authentication__Authority: props.serviceProps.cloudfrontDistribution.distributionDomainName,
        Authentication__MetadataAddress: props.serviceProps.cloudfrontDistribution.distributionDomainName,
        Authentication__Mode: "OidcDiscovery",
        Authentication__RequireHttpsMetadata: "true",
        ...props.serviceProps.messagingConfiguration.asEnvironmentVariables(),
      },
      secrets: {
        DD_API_KEY: Secret.fromSsmParameter(
          props.sharedProps.datadog.apiKeyParameter,
        ),
        ...props.serviceProps.messagingConfiguration.asSecrets(),
      },
      path: "/api/print/{proxy+}",
      additionalPathMappings: [],
      healthCheckPath: "/api/print/v1/health",
      healthCheckCommand: {
        command: [
          "CMD-SHELL",
          `curl -f http://localhost:8080/api/print/v1/health || exit 1`,
        ],
        interval: Duration.seconds(30),
        timeout: Duration.seconds(5),
        retries: 3,
        startPeriod: Duration.seconds(60),
      },
      serviceDiscoveryNamespace: props.serviceDiscoveryNamespace,
      serviceDiscoveryName: props.serviceDiscoveryName,
      deployInPrivateSubnet: props.deployInPrivateSubnet,
      serviceDependencies: props.serviceProps.serviceDependencies,
    });

    props.printerTable.grantReadWriteData(webService.taskRole);
    props.printJobTable.grantReadWriteData(webService.taskRole);
    props.serviceProps.messagingConfiguration.grantPermissions(webService.taskRole);
  }
}
