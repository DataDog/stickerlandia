/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import * as cdk from "aws-cdk-lib";
import { Construct } from "constructs";
import { Network } from "./network";
import { CorsHttpMethod, HttpApi } from "aws-cdk-lib/aws-apigatewayv2";
import { PrivateDnsNamespace } from "aws-cdk-lib/aws-servicediscovery";
import { StringParameter } from "aws-cdk-lib/aws-ssm";
import { EventBus } from "aws-cdk-lib/aws-events";
// import * as sqs from 'aws-cdk-lib/aws-sqs';

export class StickerlandiaSharedResourcesStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    const env = process.env.ENV || "dev";

    const network = new Network(this, "Network", {
      env,
      account: this.account,
    });

    const dnsNamespace = new PrivateDnsNamespace(this, "PrivateDnsNamespace", {
      name: `${env}.stickerlandia.local`,
      vpc: network.vpc,
    });

    new StringParameter(this, "DnsNamespaceIdParam", {
      stringValue: dnsNamespace.namespaceId,
      parameterName: `/stickerlandia/${env}/shared/namespace-id`,
    });
    new StringParameter(this, "DnsNamespaceNameParam", {
      stringValue: dnsNamespace.namespaceName,
      parameterName: `/stickerlandia/${env}/shared/namespace-name`,
    });
    new StringParameter(this, "DnsNamespaceArnParam", {
      stringValue: dnsNamespace.namespaceArn,
      parameterName: `/stickerlandia/${env}/shared/namespace-arn`,
    });

    new StringParameter(this, "HttpApiId", {
      stringValue: network.httpApi.httpApiId,
      parameterName: `/stickerlandia/${env}/shared/api-id`,
    });
    new StringParameter(this, "VpcLinkId", {
      stringValue: network.vpcLink.vpcLinkId,
      parameterName: `/stickerlandia/${env}/shared/vpc-link-id`,
    });

    const eventBus = new EventBus(this, "StickerlandiaSharedEventBus", {
      eventBusName: `stickerlandia-${env}-event-bus`,
    });

    const eventBusName = new StringParameter(this, "EventBusNameParam", {
      stringValue: eventBus.eventBusName,
      parameterName: `/stickerlandia/${env}/shared/eb-name`,
    });

    const ebArnParam = new StringParameter(this, "EventBusArnParam", {
      stringValue: eventBus.eventBusArn,
      parameterName: `/stickerlandia/${env}/shared/eb-arn`,
    });

    const loadBalancerArn = new StringParameter(this, "LoadBalancerArnParam", {
      stringValue: network.loadBalancer.loadBalancerArn,
      parameterName: `/stickerlandia/${env}/shared/alb-arn`,
    });

    const httpsListenerArn = new StringParameter(
      this,
      "HttpsListenerArnParam",
      {
        stringValue: network.applicationListener.listenerArn,
        parameterName: `/stickerlandia/${env}/shared/https-listener-arn`,
      }
    );
  }
}
