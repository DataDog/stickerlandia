/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

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
import { VpcLink } from "aws-cdk-lib/aws-apigatewayv2";
import { Construct } from "constructs";

export interface NetworkProps {
  env: string;
}

export class Network extends Construct {
  vpc: IVpc;
  vpcLink: VpcLink;
  noInboundAllOutboundSecurityGroup: SecurityGroup;

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
  }
}
