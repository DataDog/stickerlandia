/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import * as cdk from "aws-cdk-lib";
import { IVpc } from "aws-cdk-lib/aws-ec2";
import { IStringParameter, StringParameter } from "aws-cdk-lib/aws-ssm";
import { Construct } from "constructs";
import {
  ClusterInstance,
  DatabaseCluster,
  DatabaseSecret,
} from "aws-cdk-lib/aws-rds";

export interface PersistenceProps {
  env: string;
  account: string;
  vpc: IVpc;
}

export class Persistence extends Construct {
  databaseEndpoint: IStringParameter;

  constructor(scope: Construct, id: string, props: PersistenceProps) {
    super(scope, id);

    var databaseSecurityGroup = new cdk.aws_ec2.SecurityGroup(
      this,
      "DatabaseSecurityGroup",
      {
        vpc: props.vpc,
        description: "Security group for Stickerlandia database",
        allowAllOutbound: true,
      }
    );
    databaseSecurityGroup.addIngressRule(
      cdk.aws_ec2.Peer.ipv4(props.vpc.vpcCidrBlock),
      cdk.aws_ec2.Port.tcp(5432),
      "Allow Postgres access from within the VPC"
    );

    var secret = new DatabaseSecret(this, "SharedDBSecret", {
      username: "postgres",
      excludeCharacters: '"@/\\',
    });

    // Use DESTROY for dev environments, RETAIN for production
    const removalPolicy = props.env === "dev"
      ? cdk.RemovalPolicy.DESTROY
      : cdk.RemovalPolicy.RETAIN;

    var cluster = new DatabaseCluster(this, "SharedDB", {
      clusterIdentifier: `stickerlandia-${props.env}-db`,
      engine: cdk.aws_rds.DatabaseClusterEngine.auroraPostgres({
        version: cdk.aws_rds.AuroraPostgresEngineVersion.VER_17_4,
      }),
      vpc: props.vpc,
      credentials: cdk.aws_rds.Credentials.fromSecret(secret),
      serverlessV2MinCapacity: 1,
      serverlessV2MaxCapacity: 1,
      securityGroups: [databaseSecurityGroup],
      removalPolicy: removalPolicy,
      deletionProtection: props.env !== "dev",
      defaultDatabaseName: "stickerlandia",
      writer: cdk.aws_rds.ClusterInstance.serverlessV2(
        "StickerlandiaWriterInstance"
      ),
      readers: [
        ClusterInstance.serverlessV2("StickerlandiaReaderInstance", {
          scaleWithWriter: true,
        }),
      ],
    });

    var databaseEndpointParam = new StringParameter(
      this,
      "DatabaseEndpointParam",
      {
        parameterName: `/stickerlandia/${props.env}/shared/database-endpoint`,
        stringValue: cluster.clusterEndpoint.hostname,
        description: `The database endpoint for the Stickerlandia ${props.env} environment`,
        tier: cdk.aws_ssm.ParameterTier.STANDARD,
      }
    );

    var databaseIdentifierParam = new StringParameter(
      this,
      "DatabaseIdentifierParam",
      {
        parameterName: `/stickerlandia/${props.env}/shared/database-identifier`,
        stringValue: cluster.clusterIdentifier,
        description: `The database ARN for the Stickerlandia ${props.env} environment`,
        tier: cdk.aws_ssm.ParameterTier.STANDARD,
      }
    );

    var databaseResourceIdentifierParam = new StringParameter(this, "DatabaseResourceIdentifierParam", {
      parameterName: `/stickerlandia/${props.env}/shared/database-resource-identifier`,
      stringValue: cluster.clusterResourceIdentifier,
      description: `The database ARN for the Stickerlandia ${props.env} environment`,
      tier: cdk.aws_ssm.ParameterTier.STANDARD,
    });

    // Export the secret ARN so microservices can fetch credentials
    new StringParameter(this, "DatabaseSecretArnParam", {
      parameterName: `/stickerlandia/${props.env}/shared/database-secret-arn`,
      stringValue: secret.secretArn,
      description: `The Secrets Manager ARN for the Stickerlandia ${props.env} database credentials`,
      tier: cdk.aws_ssm.ParameterTier.STANDARD,
    });
  }
}
