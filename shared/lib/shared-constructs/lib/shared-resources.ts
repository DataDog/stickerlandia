/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import {
  CorsHttpMethod,
  HttpApi,
  IHttpApi,
  IVpcLink,
  VpcLink,
} from "aws-cdk-lib/aws-apigatewayv2";
import {
  IVpc,
  Peer,
  Port,
  SecurityGroup,
  SubnetType,
  Vpc,
} from "aws-cdk-lib/aws-ec2";
import {
  ApplicationListener,
  ApplicationLoadBalancer,
  ApplicationProtocol,
  IApplicationListener,
  IApplicationLoadBalancer,
} from "aws-cdk-lib/aws-elasticloadbalancingv2";
import { IEventBus } from "aws-cdk-lib/aws-events";
import {
  IPrivateDnsNamespace,
  PrivateDnsNamespace,
} from "aws-cdk-lib/aws-servicediscovery";
import { StringParameter } from "aws-cdk-lib/aws-ssm";
import { Construct } from "constructs";
export interface SharedResourcesProps {
  environment?: string;
  networkName: string;
}

export class SharedResources extends Construct {
  vpc: IVpc;
  vpcLink: IVpcLink;
  vpcLinkSecurityGroupId: string;
  sharedEventBus: IEventBus;
  httpApi: IHttpApi;
  serviceDiscoveryNamespace: IPrivateDnsNamespace;
  applicationLoadBalancer: IApplicationLoadBalancer;
  applicationListener: IApplicationListener;
  integrationEnvironments: string[] = ["dev", "prod"];

  constructor(scope: Construct, id: string, props: SharedResourcesProps) {
    super(scope, id);

    // If a VPC is provided, use that to configure shared resources.
    if (this.integrationEnvironments.includes(props.environment || "")) {
      this.configureSharedResourcesFromParameters(props);
    } else {
      this.createSharedResourcesForEnvironment(props);
    }
  }

  configureSharedResourcesFromParameters(props: SharedResourcesProps) {
    const vpcLinkParameter = StringParameter.fromStringParameterName(
      this,
      "VpcLinkParameter",
      `/stickerlandia/${props.environment}/shared/vpc-link-id`
    );
    const vpcLinkSecurityGroupParameter =
      StringParameter.fromStringParameterName(
        this,
        "VpcLinkSecurityGroupParameter",
        `/stickerlandia/${props.environment}/shared/vpc-link-sg-id`
      );
    const httpApiParameter = StringParameter.fromStringParameterName(
      this,
      "HttpApiParameter",
      `/stickerlandia/${props.environment}/shared/api-id`
    );
    const serviceDiscoveryNamespaceIdParameter =
      StringParameter.fromStringParameterName(
        this,
        "ServiceDiscoveryNamespaceIdParameter",
        `/stickerlandia/${props.environment}/shared/namespace-id`
      );
    const serviceDiscoveryNamespaceNameParameter =
      StringParameter.fromStringParameterName(
        this,
        "ServiceDiscoveryNamespaceNameParameter",
        `/stickerlandia/${props.environment}/shared/namespace-name`
      );
    const serviceDiscoveryNamespaceArnParameter =
      StringParameter.fromStringParameterName(
        this,
        "ServiceDiscoveryNamespaceArnParameter",
        `/stickerlandia/${props.environment}/shared/namespace-arn`
      );
    const listenerArnParameter = StringParameter.fromStringParameterName(
      this,
      "ListenerArnParameter",
      `/stickerlandia/${props.environment}/shared/https-listener-arn`
    );

    const vpcId = StringParameter.valueFromLookup(
      this,
      `/stickerlandia/${props.environment}/shared/vpc-id`
    );
    const albArn = StringParameter.valueFromLookup(
      this,
      `/stickerlandia/${props.environment}/shared/alb-arn`
    );

    const vpcLinkId = vpcLinkParameter.stringValue;
    const vpcLinkSecurityGroupId = vpcLinkSecurityGroupParameter.stringValue;
    const httpApiId = httpApiParameter.stringValue;
    const serviceDiscoveryNamespaceId =
      serviceDiscoveryNamespaceIdParameter.stringValue;
    const serviceDiscoveryNamespaceName =
      serviceDiscoveryNamespaceNameParameter.stringValue;
    const serviceDiscoveryNamespaceArn =
      serviceDiscoveryNamespaceArnParameter.stringValue;
    const listenerArn = listenerArnParameter.stringValue;

    if (
      !vpcLinkId ||
      !vpcLinkSecurityGroupId ||
      !httpApiId ||
      !serviceDiscoveryNamespaceId ||
      !serviceDiscoveryNamespaceName ||
      !serviceDiscoveryNamespaceArn ||
      !albArn ||
      !listenerArn
    ) {
      throw new Error(
        "Parameters for shared resources are not set correctly."
      );
    }
    this.vpc = Vpc.fromLookup(this, "StickerlandiaVpc", {
      vpcId: vpcId,
    });
    this.vpcLink = VpcLink.fromVpcLinkAttributes(this, "VpcLink", {
      vpcLinkId: vpcLinkId,
      vpc: this.vpc,
    });
    this.vpcLinkSecurityGroupId = vpcLinkSecurityGroupId;
    this.httpApi = HttpApi.fromHttpApiAttributes(this, "HttpApi", {
      httpApiId: httpApiId,
    });
    this.serviceDiscoveryNamespace =
      PrivateDnsNamespace.fromPrivateDnsNamespaceAttributes(
        this,
        "ServiceDiscoveryNamespace",
        {
          namespaceId: serviceDiscoveryNamespaceId,
          namespaceName: serviceDiscoveryNamespaceName,
          namespaceArn: serviceDiscoveryNamespaceArn,
        }
      );

    this.applicationLoadBalancer = ApplicationLoadBalancer.fromLookup(
      this,
      "ALB",
      {
        loadBalancerArn: albArn,
      }
    );

    this.applicationListener = ApplicationListener.fromLookup(
      this,
      "HttpListener",
      {
        loadBalancerArn: albArn,
        listenerProtocol: ApplicationProtocol.HTTP,
        listenerPort: 80,
      }
    );
  }

  createSharedResourcesForEnvironment(props: SharedResourcesProps) {
    this.vpc = new Vpc(this, "Vpc", {
      vpcName: props.networkName,
      maxAzs: 2,
      natGateways: 1, // Use a single NAT Gateway for cost efficiency
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

    const noInboundAllOutboundSecurityGroup = new SecurityGroup(
      this,
      "noInboundAllOutboundSecurityGroup",
      {
        vpc: this.vpc,
        allowAllOutbound: true,
        description: "No inbound / all outbound",
        securityGroupName: "noInboundAllOutboundSecurityGroup",
      }
    );
    noInboundAllOutboundSecurityGroup.addIngressRule(
      noInboundAllOutboundSecurityGroup,
      Port.tcp(8080),
      "allow self"
    );
    noInboundAllOutboundSecurityGroup.addIngressRule(
      Peer.ipv4(this.vpc.vpcCidrBlock),
      Port.tcp(8080)
    );

    this.vpcLinkSecurityGroupId =
      noInboundAllOutboundSecurityGroup.securityGroupId;

    this.vpcLink = new VpcLink(this, "HttpApiVpcLink", {
      vpcLinkName: `Stickerlandia-${props.environment}-Users-VpcLink`,
      vpc: this.vpc,
      subnets: this.vpc.selectSubnets({
        subnetType: SubnetType.PRIVATE_WITH_EGRESS,
      }),
      securityGroups: [noInboundAllOutboundSecurityGroup],
    });

    this.httpApi = new HttpApi(this, "StickerlandiaHttpApi", {
      apiName: `Stickerlandia-Users-${props.environment}`,
      corsPreflight: {
        allowOrigins: ["*"],
        allowMethods: [CorsHttpMethod.ANY],
        allowHeaders: ["*"],
      },
    });

    this.serviceDiscoveryNamespace = new PrivateDnsNamespace(
      this,
      "PrivateDnsNamespace",
      {
        name: `${props.environment}.users.local`,
        vpc: this.vpc,
      }
    );
  }
}
