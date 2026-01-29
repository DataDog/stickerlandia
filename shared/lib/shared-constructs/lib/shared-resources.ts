/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import { RemovalPolicy } from "aws-cdk-lib";
import {
  CorsHttpMethod,
  HttpApi,
  IHttpApi,
  IVpcLink,
  VpcLink,
} from "aws-cdk-lib/aws-apigatewayv2";
import {
  AllowedMethods,
  CachePolicy,
  Distribution,
  IDistribution,
  OriginProtocolPolicy,
  OriginRequestPolicy,
  ResponseHeadersPolicy,
  SecurityPolicyProtocol,
} from "aws-cdk-lib/aws-cloudfront";
import {
  HttpOrigin,
  LoadBalancerV2Origin,
  S3BucketOrigin,
} from "aws-cdk-lib/aws-cloudfront-origins";
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
  AuroraPostgresEngineVersion,
  ClusterInstance,
  Credentials,
  DatabaseCluster,
  DatabaseClusterEngine,
  DatabaseSecret,
  IDatabaseCluster,
} from "aws-cdk-lib/aws-rds";
import {
  IPrivateDnsNamespace,
  PrivateDnsNamespace,
} from "aws-cdk-lib/aws-servicediscovery";
import { ParameterTier, StringParameter } from "aws-cdk-lib/aws-ssm";
import { Construct } from "constructs";
import * as cdk from "aws-cdk-lib";
import { CnameRecord, PublicHostedZone } from "aws-cdk-lib/aws-route53";
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
  integrationEnvironments: string[] = ["dev", "prod"];
  cloudfrontEndpoint: string;
  cloudfrontDistribution: IDistribution;
  sharedDatabaseCluster: IDatabaseCluster;
  sharedDatabaseSecretArn: string;

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
      `/stickerlandia/${props.environment}/shared/vpc-link-id`,
    );
    const vpcLinkSecurityGroupParameter =
      StringParameter.fromStringParameterName(
        this,
        "VpcLinkSecurityGroupParameter",
        `/stickerlandia/${props.environment}/shared/vpc-link-sg-id`,
      );
    const httpApiParameter = StringParameter.fromStringParameterName(
      this,
      "HttpApiParameter",
      `/stickerlandia/${props.environment}/shared/api-id`,
    );
    const serviceDiscoveryNamespaceIdParameter =
      StringParameter.fromStringParameterName(
        this,
        "ServiceDiscoveryNamespaceIdParameter",
        `/stickerlandia/${props.environment}/shared/namespace-id`,
      );
    const serviceDiscoveryNamespaceNameParameter =
      StringParameter.fromStringParameterName(
        this,
        "ServiceDiscoveryNamespaceNameParameter",
        `/stickerlandia/${props.environment}/shared/namespace-name`,
      );
    const serviceDiscoveryNamespaceArnParameter =
      StringParameter.fromStringParameterName(
        this,
        "ServiceDiscoveryNamespaceArnParameter",
        `/stickerlandia/${props.environment}/shared/namespace-arn`,
      );
    const cloudfrontEndpoint = StringParameter.valueFromLookup(
      this,
      `/stickerlandia/${props.environment}/shared/cloudfront-endpoint`,
    );
    const cloudfrontId = StringParameter.valueFromLookup(
      this,
      `/stickerlandia/${props.environment}/shared/cloudfront-id`,
    );

    const vpcId = StringParameter.valueFromLookup(
      this,
      `/stickerlandia/${props.environment}/shared/vpc-id`,
    );

    const sharedEventBus = StringParameter.valueFromLookup(
      this,
      `/stickerlandia/${props.environment}/shared/eb-name`,
    );

    const sharedDbClusterIdentifier = StringParameter.valueFromLookup(
      this,
      `/stickerlandia/${props.environment}/shared/database-identifier`,
    );

    const sharedDbClusterEndpoint = StringParameter.valueFromLookup(
      this,
      `/stickerlandia/${props.environment}/shared/database-endpoint`,
    );

    const sharedDbResourceIdentifier = StringParameter.valueFromLookup(
      this,
      `/stickerlandia/${props.environment}/shared/database-resource-identifier`,
    );

    const sharedDbSecretArn = StringParameter.valueFromLookup(
      this,
      `/stickerlandia/${props.environment}/shared/database-secret-arn`,
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

    if (
      !vpcId ||
      !vpcLinkId ||
      !vpcLinkSecurityGroupId ||
      !httpApiId ||
      !serviceDiscoveryNamespaceId ||
      !serviceDiscoveryNamespaceName ||
      !serviceDiscoveryNamespaceArn ||
      !cloudfrontEndpoint ||
      !cloudfrontId ||
      !sharedEventBus ||
      !sharedDbClusterIdentifier ||
      !sharedDbSecretArn
    ) {
      throw new Error("Parameters for shared resources are not set correctly.");
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
    this.sharedEventBus = EventBus.fromEventBusName(
      this,
      "SharedEventBus",
      sharedEventBus,
    );
    this.sharedDatabaseCluster = DatabaseCluster.fromDatabaseClusterAttributes(
      this,
      "SharedDatabaseCluster",
      {
        clusterIdentifier: sharedDbClusterIdentifier,
        clusterEndpointAddress: sharedDbClusterEndpoint,
        clusterResourceIdentifier: sharedDbResourceIdentifier,
      },
    );
    this.sharedDatabaseSecretArn = sharedDbSecretArn;
    this.serviceDiscoveryNamespace =
      PrivateDnsNamespace.fromPrivateDnsNamespaceAttributes(
        this,
        "ServiceDiscoveryNamespace",
        {
          namespaceId: serviceDiscoveryNamespaceId,
          namespaceName: serviceDiscoveryNamespaceName,
          namespaceArn: serviceDiscoveryNamespaceArn,
        },
      );
    this.cloudfrontDistribution = Distribution.fromDistributionAttributes(
      this,
      "StickerlandiaCloudfrontDistribution",
      {
        distributionId: cloudfrontId,
        domainName: cloudfrontEndpoint,
      },
    );
    this.cloudfrontEndpoint = cloudfrontEndpoint;
  }

  createSharedResourcesForEnvironment(props: SharedResourcesProps) {
    const hostedZoneId = process.env.HOSTED_ZONE_ID;

    if (hostedZoneId === undefined) {
      throw new Error("HOSTED_ZONE_ID environment variable must be set.");
    }

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
      },
    );
    noInboundAllOutboundSecurityGroup.addIngressRule(
      noInboundAllOutboundSecurityGroup,
      Port.tcp(8080),
      "allow self",
    );
    noInboundAllOutboundSecurityGroup.addIngressRule(
      Peer.ipv4(this.vpc.vpcCidrBlock),
      Port.tcp(8080),
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
      },
    );

    this.sharedEventBus = new EventBus(this, "SharedEventBus", {
      eventBusName: `Stickerlandia-Shared-${props.environment}`,
    });

    var secret = new DatabaseSecret(this, "SharedDBSecret", {
      username: "postgres",
      excludeCharacters: '"@/\\',
    });

    var databaseSecurityGroup = new SecurityGroup(
      this,
      "DatabaseSecurityGroup",
      {
        vpc: this.vpc,
        description: "Security group for Stickerlandia database",
        allowAllOutbound: true,
      },
    );
    databaseSecurityGroup.addIngressRule(
      Peer.ipv4(this.vpc.vpcCidrBlock),
      Port.tcp(5432),
      "Allow Postgres access from within the VPC",
    );

    this.sharedDatabaseCluster = new DatabaseCluster(this, "SharedDB", {
      clusterIdentifier: `stickerlandia-${props.environment}-db`,
      engine: DatabaseClusterEngine.auroraPostgres({
        version: AuroraPostgresEngineVersion.VER_17_4,
      }),
      vpc: this.vpc,
      credentials: Credentials.fromSecret(secret),
      serverlessV2MinCapacity: 1,
      serverlessV2MaxCapacity: 1,
      securityGroups: [databaseSecurityGroup],
      removalPolicy: RemovalPolicy.DESTROY,
      defaultDatabaseName: "stickerlandia",
      writer: ClusterInstance.serverlessV2("StickerlandiaWriterInstance"),
      readers: [
        ClusterInstance.serverlessV2("StickerlandiaReaderInstance", {
          scaleWithWriter: true,
        }),
      ],
    });
    this.sharedDatabaseSecretArn = secret.secretArn;

    var databaseEndpointParam = new StringParameter(
      this,
      "DatabaseEndpointParam",
      {
        parameterName: `/stickerlandia/${props.environment}/shared/database-endpoint`,
        stringValue: this.sharedDatabaseCluster.clusterEndpoint.hostname,
        description: `The database endpoint for the Stickerlandia ${props.environment} environment`,
        tier: ParameterTier.STANDARD,
      },
    );

    // Export the secret ARN so microservices can fetch credentials
    new StringParameter(this, "DatabaseSecretArnParam", {
      parameterName: `/stickerlandia/${props.environment}/shared/database-secret-arn`,
      stringValue: secret.secretArn,
      description: `The Secrets Manager ARN for the Stickerlandia ${props.environment} database credentials`,
      tier: ParameterTier.STANDARD,
    });

    const region = cdk.Stack.of(this).region;

    const distribution = new Distribution(
      this,
      `Stickerlandia-${props.environment}`,
      {
        minimumProtocolVersion: SecurityPolicyProtocol.TLS_V1_2_2021,
        defaultRootObject: "index.html",
        defaultBehavior: {
          origin: new HttpOrigin(
            `${this.httpApi.apiId}.execute-api.${region}.amazonaws.com`,
            {
              protocolPolicy: OriginProtocolPolicy.HTTPS_ONLY,
            },
          ),
          cachePolicy: CachePolicy.CACHING_DISABLED,
          originRequestPolicy:
            OriginRequestPolicy.ALL_VIEWER_EXCEPT_HOST_HEADER,
          responseHeadersPolicy:
            ResponseHeadersPolicy.CORS_ALLOW_ALL_ORIGINS_WITH_PREFLIGHT_AND_SECURITY_HEADERS,
          allowedMethods: AllowedMethods.ALLOW_ALL,
        },
      },
    );

    distribution.addBehavior(
      "/.well-known*",
      new HttpOrigin(
        `${this.httpApi.apiId}.execute-api.${region}.amazonaws.com`,
        {
          protocolPolicy: OriginProtocolPolicy.HTTPS_ONLY,
        },
      ),
      {
        cachePolicy: CachePolicy.CACHING_DISABLED,
        originRequestPolicy: OriginRequestPolicy.ALL_VIEWER_EXCEPT_HOST_HEADER,
        responseHeadersPolicy:
          ResponseHeadersPolicy.CORS_ALLOW_ALL_ORIGINS_WITH_PREFLIGHT_AND_SECURITY_HEADERS,
        allowedMethods: AllowedMethods.ALLOW_ALL,
      },
    );

    distribution.addBehavior(
      "/auth*",
      new HttpOrigin(
        `${this.httpApi.apiId}.execute-api.${region}.amazonaws.com`,
        {
          protocolPolicy: OriginProtocolPolicy.HTTPS_ONLY,
        },
      ),
      {
        cachePolicy: CachePolicy.CACHING_DISABLED,
        originRequestPolicy: OriginRequestPolicy.ALL_VIEWER_EXCEPT_HOST_HEADER,
        responseHeadersPolicy:
          ResponseHeadersPolicy.CORS_ALLOW_ALL_ORIGINS_WITH_PREFLIGHT_AND_SECURITY_HEADERS,
        allowedMethods: AllowedMethods.ALLOW_ALL,
      },
    );

    const hostedZone = PublicHostedZone.fromPublicHostedZoneAttributes(
      this,
      "StickerlandiaHostedZone",
      {
        hostedZoneId: hostedZoneId,
        zoneName: "stickerlandia.dev",
      },
    );

    const cNameRecord = new CnameRecord(this, "CnameRecord", {
      zone: hostedZone,
      domainName: distribution.domainName,
      recordName: props.environment,
      ttl: cdk.Duration.minutes(5),
    });

    this.cloudfrontDistribution = distribution;
    this.cloudfrontEndpoint = distribution.distributionDomainName;
  }
}
