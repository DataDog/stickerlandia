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
import { EventBus, IEventBus } from "aws-cdk-lib/aws-events";
import {
  IPrivateDnsNamespace,
  PrivateDnsNamespace,
} from "aws-cdk-lib/aws-servicediscovery";
import { Construct } from "constructs";
export interface SharedResourcesProps {
  environment?: string;
  networkName: string;
  existingVpcId: string | undefined;
}

export class SharedResources extends Construct {
  vpc: IVpc;
  vpcLink: IVpcLink;
  vpcLinkSecurityGroupId: string;
  sharedEventBus: IEventBus;
  httpApi: IHttpApi;
  serviceDiscoveryNamespace: IPrivateDnsNamespace;

  constructor(scope: Construct, id: string, props: SharedResourcesProps) {
    super(scope, id);

    this.sharedEventBus = new EventBus(this, "UserManagementEventBus", {
      eventBusName: `stickerlandia-shared-event-bus`,
    });

    // If a VPC is provided, use that to configure shared resources.
    if (props.existingVpcId) {
      this.configureSharedResourcesFromEnvironment(props);
    } else {
      this.createSharedResourcesForEnvironment(props);
    }
  }

  configureSharedResourcesFromEnvironment(props: SharedResourcesProps) {
    const vpcLinkId = process.env.VPC_LINK_ID;
    const vpcLinkSecurityGroupId = process.env.VPC_LINK_SECURITY_GROUP_ID;
    const httpApiId = process.env.HTTP_API_ID;
    const serviceDiscoveryNamespaceId =
      process.env.SERVICE_DISCOVERY_NAMESPACE_ID;
    const serviceDiscoveryNamespaceName =
      process.env.SERVICE_DISCOVERY_NAMESPACE_NAME;
    const serviceDiscoveryNamespaceArn =
      process.env.SERVICE_DISCOVERY_NAMESPACE_ARN;

    if (
      !vpcLinkId ||
      !vpcLinkSecurityGroupId ||
      !httpApiId ||
      !serviceDiscoveryNamespaceId ||
      !serviceDiscoveryNamespaceName ||
      !serviceDiscoveryNamespaceArn
    ) {
      throw new Error(
        "Environment variables for shared resources are not set correctly. Required environment variables: VPC_LINK_ID, VPC_LINK_SECURITY_GROUP_ID, HTTP_API_ID, SERVICE_DISCOVERY_NAMESPACE_ID, SERVICE_DISCOVERY_NAMESPACE_NAME, SERVICE_DISCOVERY_NAMESPACE_ARN."
      );
    }
    this.vpc = Vpc.fromLookup(this, "StickerlandiaVpc", {
      vpcId: props.existingVpcId,
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
