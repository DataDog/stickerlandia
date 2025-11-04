/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import * as cdk from "aws-cdk-lib";
import {
  CfnRoute,
  FlowLogDestination,
  GatewayVpcEndpointAwsService,
  IVpc,
  Peer,
  Port,
  SecurityGroup,
  SubnetType,
  Vpc,
} from "aws-cdk-lib/aws-ec2";
import { StringParameter } from "aws-cdk-lib/aws-ssm";
import { CorsHttpMethod, HttpApi, VpcLink } from "aws-cdk-lib/aws-apigatewayv2";
import { Construct } from "constructs";
import {
  AllowedMethods,
  CachePolicy,
  Distribution,
  OriginAccessIdentity,
  OriginProtocolPolicy,
  OriginRequestPolicy,
  ResponseHeadersPolicy,
  SecurityPolicyProtocol,
} from "aws-cdk-lib/aws-cloudfront";
import {
  ApplicationListener,
  ApplicationLoadBalancer,
  ApplicationProtocol,
  ListenerAction,
  SslPolicy,
} from "aws-cdk-lib/aws-elasticloadbalancingv2";
import {
  HttpOrigin,
  LoadBalancerV2Origin,
  S3BucketOrigin,
} from "aws-cdk-lib/aws-cloudfront-origins";

export interface NetworkProps {
  env: string;
  account: string;
}

export class Network extends Construct {
  vpc: IVpc;
  vpcLink: VpcLink;
  noInboundAllOutboundSecurityGroup: SecurityGroup;
  loadBalancer: ApplicationLoadBalancer;
  applicationListener: ApplicationListener;
  httpApi: cdk.aws_apigatewayv2.HttpApi;
  distribution: Distribution;

  constructor(scope: Construct, id: string, props: NetworkProps) {
    super(scope, id);
    this.vpc = new Vpc(this, "Vpc", {
      vpcName: `stickerlandia-${props.env}-vpc`,
      maxAzs: 2,
      natGateways: 1,
      subnetConfiguration: [
        {
          cidrMask: 24,
          name: "Public",
          subnetType: SubnetType.PUBLIC,
        },
        {
          cidrMask: 24,
          name: "Private",
          subnetType: SubnetType.PRIVATE_WITH_EGRESS,
        },
        {
          cidrMask: 24,
          name: "Isolated",
          subnetType: SubnetType.PRIVATE_ISOLATED,
        },
      ],
    });

    this.vpc.addGatewayEndpoint("dynamoDBEndpoint", {
      service: GatewayVpcEndpointAwsService.DYNAMODB,
    });
    this.vpc.addGatewayEndpoint("s3Endpoint", {
      service: GatewayVpcEndpointAwsService.S3,
    });

    this.noInboundAllOutboundSecurityGroup = new SecurityGroup(
      this,
      "noInboundAllOutboundSecurityGroup",
      {
        vpc: this.vpc,
        allowAllOutbound: true,
        description: "No inbound / all outbound",
        securityGroupName: "noInboundAllOutboundSecurityGroup",
      }
    );
    this.noInboundAllOutboundSecurityGroup.addIngressRule(
      this.noInboundAllOutboundSecurityGroup,
      Port.tcp(8080),
      "allow self"
    );
    this.noInboundAllOutboundSecurityGroup.addIngressRule(
      Peer.ipv4(this.vpc.vpcCidrBlock),
      Port.tcp(8080)
    );

    this.vpcLink = new VpcLink(this, "HttpApiVpcLink", {
      vpcLinkName: `Stickerlandia-${props.env}-VpcLink`,
      vpc: this.vpc,
      subnets: this.vpc.selectSubnets({
        subnetType: SubnetType.PRIVATE_WITH_EGRESS,
      }),
      securityGroups: [this.noInboundAllOutboundSecurityGroup],
    });

    const vpcIdParameter = new StringParameter(this, "VPCIdParameter", {
      stringValue: this.vpc.vpcId,
      parameterName: `/stickerlandia/${props.env}/shared/vpc-id`,
    });

    const vpcLinkSgParameter = new StringParameter(
      this,
      "VPCLinkSecurityGroupParameter",
      {
        stringValue: this.noInboundAllOutboundSecurityGroup.securityGroupId,
        parameterName: `/stickerlandia/${props.env}/shared/vpc-link-sg-id`,
      }
    );

    this.httpApi = new HttpApi(this, "StickerlandiaHttpApi", {
      apiName: `Stickerlandia-${props.env}`,
      corsPreflight: {
        allowOrigins: ["*"],
        allowMethods: [CorsHttpMethod.ANY],
        allowHeaders: ["*"],
      },
    });

    const webFrontendBucket = new cdk.aws_s3.Bucket(this, "WebFrontendBucket", {
      bucketName: `stickerlandia-web-frontend-${props.env}-${props.account}`,
      removalPolicy: cdk.RemovalPolicy.DESTROY,
      accessControl: cdk.aws_s3.BucketAccessControl.PRIVATE,
      autoDeleteObjects: true,
      websiteIndexDocument: "index.html",
    });

    const originIdentity = new OriginAccessIdentity(this, "OAI", {
      comment: `OAI for stickerlandia web frontend ${props.env}`,
    });
    webFrontendBucket.grantRead(originIdentity);

    this.distribution = new Distribution(this, `Stickerlandia-${props.env}`, {
      minimumProtocolVersion: SecurityPolicyProtocol.TLS_V1_2_2021,
      defaultRootObject: "index.html",
      defaultBehavior: {
        origin: S3BucketOrigin.withOriginAccessIdentity(webFrontendBucket, {
          originAccessIdentity: originIdentity,
        }),
      },
    });

    this.distribution.addBehavior(
      "/api*",
      new HttpOrigin(
        `${this.httpApi.apiId}.execute-api.eu-west-1.amazonaws.com`,
        {
          protocolPolicy: OriginProtocolPolicy.HTTPS_ONLY,
        }
      ),
      {
        cachePolicy: CachePolicy.CACHING_DISABLED,
        originRequestPolicy: OriginRequestPolicy.ALL_VIEWER_EXCEPT_HOST_HEADER,
        responseHeadersPolicy:
          ResponseHeadersPolicy.CORS_ALLOW_ALL_ORIGINS_WITH_PREFLIGHT_AND_SECURITY_HEADERS,
        allowedMethods: AllowedMethods.ALLOW_ALL,
      }
    );

    this.distribution.addBehavior(
      "/.well-known*",
      new HttpOrigin(
        `${this.httpApi.apiId}.execute-api.eu-west-1.amazonaws.com`,
        {
          protocolPolicy: OriginProtocolPolicy.HTTPS_ONLY,
        }
      ),
      {
        cachePolicy: CachePolicy.CACHING_DISABLED,
        originRequestPolicy: OriginRequestPolicy.ALL_VIEWER_EXCEPT_HOST_HEADER,
        responseHeadersPolicy:
          ResponseHeadersPolicy.CORS_ALLOW_ALL_ORIGINS_WITH_PREFLIGHT_AND_SECURITY_HEADERS,
        allowedMethods: AllowedMethods.ALLOW_ALL,
      }
    );

    this.distribution.addBehavior(
      "/auth*",
      new HttpOrigin(
        `${this.httpApi.apiId}.execute-api.eu-west-1.amazonaws.com`,
        {
          protocolPolicy: OriginProtocolPolicy.HTTPS_ONLY,
        }
      ),
      {
        cachePolicy: CachePolicy.CACHING_DISABLED,
        originRequestPolicy: OriginRequestPolicy.ALL_VIEWER_EXCEPT_HOST_HEADER,
        responseHeadersPolicy:
          ResponseHeadersPolicy.CORS_ALLOW_ALL_ORIGINS_WITH_PREFLIGHT_AND_SECURITY_HEADERS,
        allowedMethods: AllowedMethods.ALLOW_ALL,
      }
    );

    this.distribution.addBehavior(
      "/Auth*",
      new HttpOrigin(
        `${this.httpApi.apiId}.execute-api.eu-west-1.amazonaws.com`,
        {
          protocolPolicy: OriginProtocolPolicy.HTTPS_ONLY,
        }
      ),
      {
        cachePolicy: CachePolicy.CACHING_DISABLED,
        originRequestPolicy: OriginRequestPolicy.ALL_VIEWER_EXCEPT_HOST_HEADER,
        responseHeadersPolicy:
          ResponseHeadersPolicy.CORS_ALLOW_ALL_ORIGINS_WITH_PREFLIGHT_AND_SECURITY_HEADERS,
        allowedMethods: AllowedMethods.ALLOW_ALL,
      }
    );
  }
}
