/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import { Construct } from "constructs";
import {
  PolicyStatement,
  Effect,
  IGrantable,
} from "aws-cdk-lib/aws-iam";
import { CustomResource, Duration, RemovalPolicy, Stack } from "aws-cdk-lib";
import {
  Function as LambdaFunction,
  Runtime,
  Code,
} from "aws-cdk-lib/aws-lambda";
import { Provider } from "aws-cdk-lib/custom-resources";
import { ISecret, Secret } from "aws-cdk-lib/aws-secretsmanager";
import { IStringParameter, StringParameter } from "aws-cdk-lib/aws-ssm";
import { Secret as EcsSecret } from "aws-cdk-lib/aws-ecs";
import { IVpc, SubnetType } from "aws-cdk-lib/aws-ec2";
import * as path from "path";

export enum ConnectionStringFormat {
  /** .NET format: Host=xxx;Database=xxx;Username=xxx;Password=xxx */
  DOTNET = "dotnet",
  /** Go/PostgreSQL URL format: postgres://user:pass@host:5432/db?sslmode=require */
  POSTGRES_URL = "postgres_url",
  /** Individual fields (for Quarkus JDBC) - creates separate params for host, username, password */
  INDIVIDUAL_FIELDS = "individual_fields",
}

export interface DatabaseCredentialsProps {
  /** The Secrets Manager secret ARN containing RDS credentials */
  databaseSecretArn: string;
  /** The environment name (dev, prod, etc.) */
  environment: string;
  /** The service name for secret paths */
  serviceName: string;
  /** The connection string format to generate */
  format: ConnectionStringFormat;
  /** The database name to use - each service should use a unique database name */
  databaseName: string;
  /** The VPC to run the Lambda in (required to connect to RDS) */
  vpc: IVpc;
  /**
   * Whether to create SSM parameter references for Lambda functions.
   * Set to true if using Lambda (which needs SSM params resolved at deploy time).
   * Set to false for ECS-only services (which use Secrets Manager instead).
   * Defaults to false to avoid CloudFormation validation errors.
   */
  createSsmParameterReferences?: boolean;
}

/**
 * Creates formatted database connection string secrets from an RDS secret.
 *
 * This construct reads the RDS secret (which contains JSON with host, username, password, etc.)
 * and creates Secrets Manager secrets with connection strings formatted for specific platforms.
 *
 * Using Secrets Manager (instead of SSM Parameters) avoids CloudFormation's upfront parameter
 * validation, which would fail because the secret doesn't exist until the CustomResource runs.
 */
export class DatabaseCredentials extends Construct {
  /** The Secrets Manager secret containing the formatted connection string (for DOTNET and POSTGRES_URL formats) - use for ECS */
  public readonly connectionStringSecret?: ISecret;
  /** For INDIVIDUAL_FIELDS format - the JDBC URL secret */
  public readonly jdbcUrlSecret?: ISecret;
  /** For INDIVIDUAL_FIELDS format - the username secret */
  public readonly usernameSecret?: ISecret;
  /** For INDIVIDUAL_FIELDS format - the password secret */
  public readonly passwordSecret?: ISecret;
  /** The SSM parameter containing the formatted connection string - use for Lambda (resolved at deploy time) */
  public readonly connectionStringParameter?: IStringParameter;
  /** For INDIVIDUAL_FIELDS format - the JDBC URL SSM parameter */
  public readonly jdbcUrlParameter?: IStringParameter;
  /** For INDIVIDUAL_FIELDS format - the username SSM parameter */
  public readonly usernameParameter?: IStringParameter;
  /** For INDIVIDUAL_FIELDS format - the password SSM parameter */
  public readonly passwordParameter?: IStringParameter;
  /** The CustomResource that creates the secrets - use this to add CloudFormation dependencies */
  public readonly credentialResource: CustomResource;
  /** The wildcard ARN pattern for the secrets created by this construct - use for IAM policies */
  public readonly secretArnPattern: string;
  /** The secret name (not ARN) - use for ECS secrets where partial ARNs don't work */
  public readonly connectionStringSecretName?: string;
  /** The SSM parameter path for the connection string - use for Lambda CloudFormation dynamic references */
  public readonly connectionStringSsmPath?: string;

  constructor(scope: Construct, id: string, props: DatabaseCredentialsProps) {
    super(scope, id);

    const databaseName = props.databaseName;
    const secretNamePrefix = `stickerlandia/${props.environment}/${props.serviceName}`;
    const region = Stack.of(this).region;
    const account = Stack.of(this).account;

    // Store the wildcard ARN pattern for use by grantRead()
    // The -?????? suffix is needed because Secrets Manager adds a random 6-character suffix to secret ARNs
    this.secretArnPattern = `arn:aws:secretsmanager:${region}:${account}:secret:${secretNamePrefix}/*`;

    // Lambda function to read RDS secret, create the database, and create formatted connection string secrets
    const handler = new LambdaFunction(this, "CredentialFormatterHandler", {
      runtime: Runtime.NODEJS_20_X,
      handler: "index.handler",
      timeout: Duration.seconds(60),
      vpc: props.vpc,
      vpcSubnets: { subnetType: SubnetType.PRIVATE_WITH_EGRESS },
      code: Code.fromAsset(path.join(__dirname, "lambda/database-init"), {
        bundling: {
          image: Runtime.NODEJS_20_X.bundlingImage,
          command: [
            "bash",
            "-c",
            "npm install --cache /tmp/.npm && cp -r . /asset-output/",
          ],
        },
      }),
    });

    const ssmBasePath = `/stickerlandia/${props.environment}/${props.serviceName}`;

    // Grant permissions to read the source secret and manage destination secrets
    handler.addToRolePolicy(
      new PolicyStatement({
        effect: Effect.ALLOW,
        actions: ["secretsmanager:GetSecretValue"],
        resources: [props.databaseSecretArn],
      })
    );
    handler.addToRolePolicy(
      new PolicyStatement({
        effect: Effect.ALLOW,
        actions: [
          "secretsmanager:CreateSecret",
          "secretsmanager:UpdateSecret",
          "secretsmanager:DeleteSecret",
          "secretsmanager:GetSecretValue",
        ],
        resources: [`arn:aws:secretsmanager:${region}:${account}:secret:${secretNamePrefix}/*`],
      })
    );
    handler.addToRolePolicy(
      new PolicyStatement({
        effect: Effect.ALLOW,
        actions: [
          "ssm:PutParameter",
          "ssm:DeleteParameter",
        ],
        resources: [`arn:aws:ssm:${region}:${account}:parameter${ssmBasePath}/*`],
      })
    );

    // Create the custom resource provider
    const provider = new Provider(this, "CredentialFormatterProvider", {
      onEventHandler: handler,
    });

    // Create the custom resource
    this.credentialResource = new CustomResource(this, "CredentialFormatter", {
      serviceToken: provider.serviceToken,
      properties: {
        SourceSecretArn: props.databaseSecretArn,
        Format: props.format,
        DatabaseName: databaseName,
        SecretNamePrefix: secretNamePrefix,
        SsmBasePath: ssmBasePath,
        // Add a version to force updates when needed
        Version: Date.now().toString(),
      },
      removalPolicy: RemovalPolicy.DESTROY,
    });

    // Create references to Secrets Manager secrets (for ECS - no upfront validation)
    // SSM parameter references are only created when explicitly requested (for Lambda),
    // as they can cause CloudFormation validation errors if the parameter doesn't exist yet.
    if (props.format === ConnectionStringFormat.INDIVIDUAL_FIELDS) {
      this.jdbcUrlSecret = Secret.fromSecretNameV2(
        this,
        "JdbcUrlSecret",
        `${secretNamePrefix}/database-host`
      );
      this.usernameSecret = Secret.fromSecretNameV2(
        this,
        "UsernameSecret",
        `${secretNamePrefix}/database-user`
      );
      this.passwordSecret = Secret.fromSecretNameV2(
        this,
        "PasswordSecret",
        `${secretNamePrefix}/database-password`
      );
      // Only create SSM parameter references if explicitly requested (for Lambda)
      if (props.createSsmParameterReferences) {
        this.jdbcUrlParameter = StringParameter.fromStringParameterName(
          this,
          "JdbcUrlParam",
          `${ssmBasePath}/database-host`
        );
        this.usernameParameter = StringParameter.fromStringParameterName(
          this,
          "UsernameParam",
          `${ssmBasePath}/database-user`
        );
        this.passwordParameter = StringParameter.fromStringParameterName(
          this,
          "PasswordParam",
          `${ssmBasePath}/database-password`
        );
      }
    } else {
      this.connectionStringSecretName = `${secretNamePrefix}/connection-string`;
      this.connectionStringSsmPath = `${ssmBasePath}/connection_string`;
      this.connectionStringSecret = Secret.fromSecretNameV2(
        this,
        "ConnectionStringSecret",
        this.connectionStringSecretName
      );
      // Only create SSM parameter reference if explicitly requested (for Lambda)
      if (props.createSsmParameterReferences) {
        this.connectionStringParameter = StringParameter.fromStringParameterName(
          this,
          "ConnectionStringParam",
          `${ssmBasePath}/connection_string`
        );
      }
    }
  }

  /**
   * Grant the given grantable permission to read secrets created by this construct.
   * This is necessary because Secret.fromSecretNameV2() doesn't include the random
   * suffix that Secrets Manager adds to ARNs, so CDK's automatic grant methods
   * may not work correctly for dynamically created secrets.
   *
   * @param grantee The IAM principal to grant read access to (e.g., ECS task execution role)
   */
  public grantRead(grantee: IGrantable): void {
    grantee.grantPrincipal.addToPrincipalPolicy(
      new PolicyStatement({
        effect: Effect.ALLOW,
        actions: ["secretsmanager:GetSecretValue"],
        resources: [this.secretArnPattern],
      })
    );
  }

  /**
   * Get an ECS Secret for the connection string that uses the full ARN (with random suffix).
   *
   * This is necessary because Secret.fromSecretNameV2() produces a partial ARN
   * (without the random suffix), which ECS cannot resolve. ECS requires the full ARN
   * for Secrets Manager secrets.
   *
   * This method returns an ECS Secret that uses the full ARN from the CustomResource output.
   *
   * @returns An ECS Secret for use in container definitions, or undefined if not using connection string format
   */
  public getConnectionStringEcsSecret(): EcsSecret | undefined {
    if (!this.connectionStringSecretName) {
      return undefined;
    }
    // Get the full ARN (with random suffix) from the CustomResource output
    const fullArn = this.credentialResource.getAttString("ConnectionStringArn");
    // Create a custom ECS Secret that uses the full ARN
    return {
      arn: fullArn,
      hasField: false,
      grantRead: (grantee) => this.grantRead(grantee),
    } as EcsSecret;
  }

  /**
   * Get the connection string value for use in Lambda environment variables.
   *
   * This returns a CloudFormation token that resolves to the connection string value
   * from the CustomResource output. The value is resolved at deploy time, not during
   * template validation, so it works even though the secret doesn't exist until the
   * CustomResource runs.
   *
   * Note: This puts the credential value in the CloudFormation template. For ECS,
   * prefer using getConnectionStringEcsSecret() which uses Secrets Manager ARN references.
   *
   * @returns A string token for the connection string value
   */
  public getConnectionStringForLambda(): string {
    return this.credentialResource.getAttString("ConnectionStringValue");
  }
}
