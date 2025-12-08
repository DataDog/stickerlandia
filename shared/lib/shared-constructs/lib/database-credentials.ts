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
  /** The database name to use (defaults to 'stickerlandia') */
  databaseName?: string;
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

    const databaseName = props.databaseName ?? "stickerlandia";
    const secretNamePrefix = `stickerlandia/${props.environment}/${props.serviceName}`;
    const region = Stack.of(this).region;
    const account = Stack.of(this).account;

    // Store the wildcard ARN pattern for use by grantRead()
    // The -?????? suffix is needed because Secrets Manager adds a random 6-character suffix to secret ARNs
    this.secretArnPattern = `arn:aws:secretsmanager:${region}:${account}:secret:${secretNamePrefix}/*`;

    // Lambda function to read RDS secret and create formatted connection string secrets AND SSM parameters
    const handler = new LambdaFunction(this, "CredentialFormatterHandler", {
      runtime: Runtime.NODEJS_20_X,
      handler: "index.handler",
      timeout: Duration.seconds(30),
      code: Code.fromInline(`
const { SecretsManagerClient, GetSecretValueCommand, CreateSecretCommand, UpdateSecretCommand, DeleteSecretCommand } = require("@aws-sdk/client-secrets-manager");
const { SSMClient, PutParameterCommand, DeleteParameterCommand } = require("@aws-sdk/client-ssm");

async function createOrUpdateSecret(client, secretName, secretValue, description) {
  let arn;
  try {
    const result = await client.send(new UpdateSecretCommand({
      SecretId: secretName,
      SecretString: secretValue,
      Description: description,
    }));
    arn = result.ARN;
    console.log(\`Updated secret: \${secretName}, ARN: \${arn}\`);
  } catch (e) {
    if (e.name === "ResourceNotFoundException") {
      const result = await client.send(new CreateSecretCommand({
        Name: secretName,
        SecretString: secretValue,
        Description: description,
      }));
      arn = result.ARN;
      console.log(\`Created secret: \${secretName}, ARN: \${arn}\`);
    } else {
      throw e;
    }
  }
  return arn;
}

async function createOrUpdateSsmParam(client, paramName, paramValue, description) {
  await client.send(new PutParameterCommand({
    Name: paramName,
    Value: paramValue,
    Type: "String",
    Overwrite: true,
    Description: description,
  }));
  console.log(\`Created/updated SSM param: \${paramName}\`);
}

exports.handler = async (event) => {
  console.log("Event:", JSON.stringify(event, null, 2));

  const sourceSecretArn = event.ResourceProperties.SourceSecretArn;
  const format = event.ResourceProperties.Format;
  const databaseName = event.ResourceProperties.DatabaseName;
  const secretNamePrefix = event.ResourceProperties.SecretNamePrefix;
  const ssmBasePath = event.ResourceProperties.SsmBasePath;

  const smClient = new SecretsManagerClient({});
  const ssmClient = new SSMClient({});

  if (event.RequestType === "Delete") {
    // Clean up secrets
    const secretsToDelete = format === "individual_fields"
      ? [\`\${secretNamePrefix}/database-host\`, \`\${secretNamePrefix}/database-user\`, \`\${secretNamePrefix}/database-password\`]
      : [\`\${secretNamePrefix}/connection-string\`];

    for (const secretName of secretsToDelete) {
      try {
        await smClient.send(new DeleteSecretCommand({ SecretId: secretName, ForceDeleteWithoutRecovery: true }));
      } catch (e) {
        if (e.name !== "ResourceNotFoundException") console.warn(\`Failed to delete secret \${secretName}:\`, e);
      }
    }

    // Clean up SSM parameters
    const paramsToDelete = format === "individual_fields"
      ? [\`\${ssmBasePath}/database-host\`, \`\${ssmBasePath}/database-user\`, \`\${ssmBasePath}/database-password\`]
      : [\`\${ssmBasePath}/connection_string\`];

    for (const paramName of paramsToDelete) {
      try {
        await ssmClient.send(new DeleteParameterCommand({ Name: paramName }));
      } catch (e) {
        if (e.name !== "ParameterNotFound") console.warn(\`Failed to delete param \${paramName}:\`, e);
      }
    }
    return { PhysicalResourceId: event.PhysicalResourceId };
  }

  // Get the source RDS secret value
  const secretResponse = await smClient.send(new GetSecretValueCommand({ SecretId: sourceSecretArn }));
  const secret = JSON.parse(secretResponse.SecretString);

  const host = secret.host;
  const username = secret.username;
  const password = secret.password;
  const port = secret.port || 5432;

  let connectionStringArn = null;
  let connectionStringValue = null;

  if (format === "dotnet") {
    const connStr = \`Host=\${host};Database=\${databaseName};Username=\${username};Password=\${password}\`;
    connectionStringValue = connStr;
    const [arn] = await Promise.all([
      createOrUpdateSecret(smClient, \`\${secretNamePrefix}/connection-string\`, connStr, \`Database connection string (auto-generated)\`),
      createOrUpdateSsmParam(ssmClient, \`\${ssmBasePath}/connection_string\`, connStr, \`Database connection string (auto-generated)\`),
    ]);
    connectionStringArn = arn;
  } else if (format === "postgres_url") {
    const encodedPassword = encodeURIComponent(password);
    const connStr = \`postgres://\${username}:\${encodedPassword}@\${host}:\${port}/\${databaseName}?sslmode=require\`;
    connectionStringValue = connStr;
    const [arn] = await Promise.all([
      createOrUpdateSecret(smClient, \`\${secretNamePrefix}/connection-string\`, connStr, \`Database connection string (auto-generated)\`),
      createOrUpdateSsmParam(ssmClient, \`\${ssmBasePath}/connection_string\`, connStr, \`Database connection string (auto-generated)\`),
    ]);
    connectionStringArn = arn;
  } else if (format === "individual_fields") {
    const jdbcUrl = \`jdbc:postgresql://\${host}:\${port}/\${databaseName}\`;
    await Promise.all([
      createOrUpdateSecret(smClient, \`\${secretNamePrefix}/database-host\`, jdbcUrl, \`JDBC URL (auto-generated)\`),
      createOrUpdateSecret(smClient, \`\${secretNamePrefix}/database-user\`, username, \`Database username (auto-generated)\`),
      createOrUpdateSecret(smClient, \`\${secretNamePrefix}/database-password\`, password, \`Database password (auto-generated)\`),
      createOrUpdateSsmParam(ssmClient, \`\${ssmBasePath}/database-host\`, jdbcUrl, \`JDBC URL (auto-generated)\`),
      createOrUpdateSsmParam(ssmClient, \`\${ssmBasePath}/database-user\`, username, \`Database username (auto-generated)\`),
      createOrUpdateSsmParam(ssmClient, \`\${ssmBasePath}/database-password\`, password, \`Database password (auto-generated)\`),
    ]);
  }

  return {
    PhysicalResourceId: \`\${secretNamePrefix}-db-creds\`,
    Data: {
      SecretNamePrefix: secretNamePrefix,
      ConnectionStringArn: connectionStringArn,
      ConnectionStringValue: connectionStringValue
    }
  };
};
      `),
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
