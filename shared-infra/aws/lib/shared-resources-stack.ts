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
    });

    const dnsNamespace = new PrivateDnsNamespace(this, "PrivateDnsNamespace", {
      name: `${env}.stickerlandia.local`,
      vpc: network.vpc,
    });

    const httpApi = new HttpApi(this, "StickerlandiaHttpApi", {
      apiName: `Stickerlandia-${env}`,
      corsPreflight: {
        allowOrigins: ["*"],
        allowMethods: [CorsHttpMethod.ANY],
        allowHeaders: ["*"],
      },
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
      stringValue: httpApi.httpApiId,
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
  }
}
