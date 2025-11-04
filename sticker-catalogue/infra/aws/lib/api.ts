/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import { IVpc } from "aws-cdk-lib/aws-ec2";
import { Construct } from "constructs";
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
import { Bucket } from "aws-cdk-lib/aws-s3";
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
  applicationLoadBalancer: IApplicationLoadBalancer;
  applicationListener: IApplicationListener;
  stickerImagesBucket: Bucket;
}

export class Api extends Construct {
  stickerClaimedQueue: Queue;
  stickerClaimedDLQ: Queue;
  userRegisteredTopic: Topic;
  constructor(scope: Construct, id: string, props: ApiProps) {
    super(scope, id);
    const webService = new WebService(this, "StickerCatalogueWebService", {
      sharedProps: props.sharedProps,
      vpc: props.vpc,
      vpcLink: props.vpcLink,
      vpcLinkSecurityGroupId: props.vpcLinkSecurityGroupId,
      httpApi: props.httpApi,
      cluster: props.cluster,
      image: "ghcr.io/datadog/stickerlandia/sticker-catalogue-service",
      imageTag: props.sharedProps.version,
      ddApiKey: props.sharedProps.datadog.apiKeyParameter,
      port: 8080,
      environmentVariables: {
        JAVA_TOOLS_OPTIONS: "-Djava.net.preferIPv4Stack=true",
        QUARKUS_DATASOURCE_JDBC_URL: props.serviceProps.jdbcUrl,
        QUARKUS_DATASOURCE_USERNAME: props.serviceProps.dbUsername,
        QUARKUS_DATASOURCE_PASSWORD: props.serviceProps.dbPassword,
        QUARKUS_DATASOURCE_DB_KIND: "postgresql",
        QUARKUS_DATASOURCE_DEVSERVICES_ENABLED: "false",
        QUARKUS_DATASOURCE_JDBC_ACQUISITION_TIMEOUT: "30S",
        KAFKA_BOOTSTRAP_SERVERS: props.serviceProps.kafkaBootstrapServers,
        MP_MESSAGING_CONNECTOR_SMALLRYE_KAFKA_BOOTSTRAP_SERVERS:
          props.serviceProps.kafkaBootstrapServers,
        QUARKUS_KAFKA_STREAMS_BOOTSTRAP_SERVERS:
          props.serviceProps.kafkaBootstrapServers,
        KAFKA_SASL_MECHANISM: "PLAIN",
        KAFKA_SECURITY_PROTOCOL: "SASL_SSL",
        KAFKA_SASL_JAAS_CONFIG:
          `org.apache.kafka.common.security.plain.PlainLoginModule required username='${props.serviceProps.kafkaUsername}' password='${props.serviceProps.kafkaPassword}';`,
        MP_MESSAGING_CONNECTOR_SMALLRYE_KAFKA_SECURITY_PROTOCOL: "SASL_SSL",
        MP_MESSAGING_CONNECTOR_SMALLRYE_KAFKA_SASL_MECHANISM: "PLAIN",
        MP_MESSAGING_CONNECTOR_SMALLRYE_KAFKA_SASL_JAAS_CONFIG:
          `org.apache.kafka.common.security.plain.PlainLoginModule required username='${props.serviceProps.kafkaUsername}' password='${props.serviceProps.kafkaPassword}';`,
        QUARKUS_S3_PATH_STYLE_ACCESS: "true",
        STICKER_IMAGES_BUCKET: props.stickerImagesBucket.bucketName,
      },
      secrets: {
        DD_API_KEY: Secret.fromSsmParameter(
          props.sharedProps.datadog.apiKeyParameter
        ),
      },
      path: "/api/stickers/v1/{proxy+}",
      healthCheckPath: "/api/stickers/v1",
      serviceDiscoveryNamespace: props.serviceDiscoveryNamespace,
      serviceDiscoveryName: props.serviceDiscoveryName,
      deployInPrivateSubnet: props.deployInPrivateSubnet,
      applicationLoadBalancer: props.applicationLoadBalancer,
      applicationListener: props.applicationListener,
    });

    props.stickerImagesBucket.grantReadWrite(webService.taskRole);
  }
}
