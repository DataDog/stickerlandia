/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import * as path from "path";
import { IVpc } from "aws-cdk-lib/aws-ec2";
import { Construct } from "constructs";
import { IDistribution } from "aws-cdk-lib/aws-cloudfront";
import { Cluster, Secret } from "aws-cdk-lib/aws-ecs";
import { Topic } from "aws-cdk-lib/aws-sns";
import { Queue } from "aws-cdk-lib/aws-sqs";
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
  cloudfrontDistribution: IDistribution;
  cloudfrontEndpoint: string;
}

export class Api extends Construct {
  stickerClaimedQueue: Queue;
  stickerClaimedDLQ: Queue;
  userRegisteredTopic: Topic;
  constructor(scope: Construct, id: string, props: ApiProps) {
    super(scope, id);
    const deploymentHostUrl = props.cloudfrontEndpoint;

    const webService = new WebService(this, "WebBackendApplication", {
      sharedProps: props.sharedProps,
      vpc: props.vpc,
      vpcLink: props.vpcLink,
      vpcLinkSecurityGroupId: props.vpcLinkSecurityGroupId,
      httpApi: props.httpApi,
      cluster: props.cluster,
      image: "ghcr.io/datadog/stickerlandia/web-backend-service",
      imageTag: props.sharedProps.version,
      assetPath: path.resolve(__dirname, "../../.."),
      ddApiKey: props.sharedProps.datadog.apiKeyParameter,
      port: 3000,
      environmentVariables: {
        NODE_ENV: "development",
        OAUTH_ISSUER_INTERNAL: deploymentHostUrl,
        OAUTH_CLIENT_ID: "web-ui",
        OAUTH_CLIENT_SECRET: "stickerlandia-web-ui-secret-2025",
        DEPLOYMENT_HOST_URL: deploymentHostUrl,
      },
      secrets: {
        DD_API_KEY: Secret.fromSsmParameter(
          props.sharedProps.datadog.apiKeyParameter,
        ),
      },
      path: "/api/app/{proxy+}",
      additionalPathMappings: [],
      healthCheckPath: "/api/app",
      serviceDiscoveryNamespace: props.serviceDiscoveryNamespace,
      serviceDiscoveryName: props.serviceDiscoveryName,
      deployInPrivateSubnet: props.deployInPrivateSubnet,
    });
  }
}
