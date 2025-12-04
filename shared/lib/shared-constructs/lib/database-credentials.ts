/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import { Construct } from "constructs";
import { StringParameter, IStringParameter } from "aws-cdk-lib/aws-ssm";
import {
  PolicyStatement,
  Effect,
} from "aws-cdk-lib/aws-iam";
import { CustomResource, Duration, RemovalPolicy } from "aws-cdk-lib";
import {
  Function as LambdaFunction,
  Runtime,
  Code,
} from "aws-cdk-lib/aws-lambda";
import { Provider } from "aws-cdk-lib/custom-resources";

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
  /** The service name for SSM parameter paths */
  serviceName: string;
  /** The connection string format to generate */
  format: ConnectionStringFormat;
  /** The database name to use (defaults to 'stickerlandia') */
  databaseName?: string;
}

/**
 * Creates formatted database connection string SSM parameters from an RDS secret.
 *
 * This construct reads the RDS secret (which contains JSON with host, username, password, etc.)
 * and creates SSM parameters with connection strings formatted for specific platforms.
 */
export class DatabaseCredentials extends Construct {
  /** The SSM parameter containing the formatted connection string (for DOTNET and POSTGRES_URL formats) */
  public readonly connectionStringParameter?: IStringParameter;
  /** For INDIVIDUAL_FIELDS format - the JDBC URL parameter */
  public readonly jdbcUrlParameter?: IStringParameter;
  /** For INDIVIDUAL_FIELDS format - the username parameter */
  public readonly usernameParameter?: IStringParameter;
  /** For INDIVIDUAL_FIELDS format - the password parameter */
  public readonly passwordParameter?: IStringParameter;

  constructor(scope: Construct, id: string, props: DatabaseCredentialsProps) {
    super(scope, id);

    const databaseName = props.databaseName ?? "stickerlandia";
    const basePath = `/stickerlandia/${props.environment}/${props.serviceName}`;

    // Lambda function to read secret and create SSM parameters
    const handler = new LambdaFunction(this, "CredentialFormatterHandler", {
      runtime: Runtime.NODEJS_20_X,
      handler: "index.handler",
      timeout: Duration.seconds(30),
      code: Code.fromInline(`
const { SecretsManagerClient, GetSecretValueCommand } = require("@aws-sdk/client-secrets-manager");
const { SSMClient, PutParameterCommand, DeleteParameterCommand } = require("@aws-sdk/client-ssm");

exports.handler = async (event) => {
  console.log("Event:", JSON.stringify(event, null, 2));

  const secretArn = event.ResourceProperties.SecretArn;
  const format = event.ResourceProperties.Format;
  const databaseName = event.ResourceProperties.DatabaseName;
  const basePath = event.ResourceProperties.BasePath;

  const smClient = new SecretsManagerClient({});
  const ssmClient = new SSMClient({});

  if (event.RequestType === "Delete") {
    // Clean up SSM parameters on delete
    const paramsToDelete = format === "individual_fields"
      ? [\`\${basePath}/database-host\`, \`\${basePath}/database-user\`, \`\${basePath}/database-password\`]
      : [\`\${basePath}/connection_string\`];

    for (const paramName of paramsToDelete) {
      try {
        await ssmClient.send(new DeleteParameterCommand({ Name: paramName }));
      } catch (e) {
        if (e.name !== "ParameterNotFound") throw e;
      }
    }
    return { PhysicalResourceId: event.PhysicalResourceId };
  }

  // Get the secret value
  const secretResponse = await smClient.send(new GetSecretValueCommand({ SecretId: secretArn }));
  const secret = JSON.parse(secretResponse.SecretString);

  const host = secret.host;
  const username = secret.username;
  const password = secret.password;
  const port = secret.port || 5432;

  if (format === "dotnet") {
    const connStr = \`Host=\${host};Database=\${databaseName};Username=\${username};Password=\${password}\`;
    await ssmClient.send(new PutParameterCommand({
      Name: \`\${basePath}/connection_string\`,
      Value: connStr,
      Type: "String",
      Overwrite: true,
      Description: \`Database connection string (auto-generated)\`
    }));
  } else if (format === "postgres_url") {
    const encodedPassword = encodeURIComponent(password);
    const connStr = \`postgres://\${username}:\${encodedPassword}@\${host}:\${port}/\${databaseName}?sslmode=require\`;
    await ssmClient.send(new PutParameterCommand({
      Name: \`\${basePath}/connection_string\`,
      Value: connStr,
      Type: "String",
      Overwrite: true,
      Description: \`Database connection string (auto-generated)\`
    }));
  } else if (format === "individual_fields") {
    const jdbcUrl = \`jdbc:postgresql://\${host}:\${port}/\${databaseName}\`;
    await Promise.all([
      ssmClient.send(new PutParameterCommand({
        Name: \`\${basePath}/database-host\`,
        Value: jdbcUrl,
        Type: "String",
        Overwrite: true,
        Description: \`JDBC URL (auto-generated)\`
      })),
      ssmClient.send(new PutParameterCommand({
        Name: \`\${basePath}/database-user\`,
        Value: username,
        Type: "String",
        Overwrite: true,
        Description: \`Database username (auto-generated)\`
      })),
      ssmClient.send(new PutParameterCommand({
        Name: \`\${basePath}/database-password\`,
        Value: password,
        Type: "String",
        Overwrite: true,
        Description: \`Database password (auto-generated)\`
      }))
    ]);
  }

  return {
    PhysicalResourceId: \`\${basePath}-db-creds\`,
    Data: { BasePath: basePath }
  };
};
      `),
    });

    // Grant permissions to read the secret and write to SSM
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
          "ssm:PutParameter",
          "ssm:DeleteParameter",
        ],
        resources: [`arn:aws:ssm:*:*:parameter${basePath}/*`],
      })
    );

    // Create the custom resource provider
    const provider = new Provider(this, "CredentialFormatterProvider", {
      onEventHandler: handler,
    });

    // Create the custom resource
    const credentialFormatter = new CustomResource(this, "CredentialFormatter", {
      serviceToken: provider.serviceToken,
      properties: {
        SecretArn: props.databaseSecretArn,
        Format: props.format,
        DatabaseName: databaseName,
        BasePath: basePath,
        // Add a version to force updates when secret changes
        Version: Date.now().toString(),
      },
      removalPolicy: RemovalPolicy.DESTROY,
    });

    // Create references to the parameters
    if (props.format === ConnectionStringFormat.INDIVIDUAL_FIELDS) {
      this.jdbcUrlParameter = StringParameter.fromStringParameterName(
        this,
        "JdbcUrlParam",
        `${basePath}/database-host`
      );
      this.usernameParameter = StringParameter.fromStringParameterName(
        this,
        "UsernameParam",
        `${basePath}/database-user`
      );
      this.passwordParameter = StringParameter.fromStringParameterName(
        this,
        "PasswordParam",
        `${basePath}/database-password`
      );
      // Add dependencies so parameters are created before being referenced
      this.jdbcUrlParameter.node.addDependency(credentialFormatter);
      this.usernameParameter.node.addDependency(credentialFormatter);
      this.passwordParameter.node.addDependency(credentialFormatter);
    } else {
      this.connectionStringParameter = StringParameter.fromStringParameterName(
        this,
        "ConnectionStringParam",
        `${basePath}/connection_string`
      );
      this.connectionStringParameter.node.addDependency(credentialFormatter);
    }
  }
}
