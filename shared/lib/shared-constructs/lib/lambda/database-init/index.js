/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

const { SecretsManagerClient, GetSecretValueCommand, CreateSecretCommand, UpdateSecretCommand, DeleteSecretCommand } = require("@aws-sdk/client-secrets-manager");
const { SSMClient, PutParameterCommand, DeleteParameterCommand } = require("@aws-sdk/client-ssm");
const { Client } = require("pg");

/**
 * Wait for the database to be ready by attempting to connect with exponential backoff.
 * This is necessary for newly created Aurora clusters which may take time to accept connections.
 */
async function waitForDatabaseReady(host, port, username, password, maxAttempts = 10, initialDelayMs = 2000) {
  let lastError;
  let delay = initialDelayMs;

  for (let attempt = 1; attempt <= maxAttempts; attempt++) {
    const client = new Client({
      host,
      port,
      user: username,
      password,
      database: "postgres",
      ssl: { rejectUnauthorized: false },
      connectionTimeoutMillis: 5000,
    });

    try {
      console.log(`Attempt ${attempt}/${maxAttempts}: Connecting to database at ${host}:${port}...`);
      await client.connect();
      console.log(`Successfully connected to database on attempt ${attempt}`);
      await client.end();
      return; // Success
    } catch (error) {
      lastError = error;
      console.log(`Attempt ${attempt}/${maxAttempts} failed: ${error.message}`);
      try {
        await client.end();
      } catch (e) {
        // Ignore cleanup errors
      }

      if (attempt < maxAttempts) {
        console.log(`Waiting ${delay}ms before next attempt...`);
        await new Promise(resolve => setTimeout(resolve, delay));
        delay = Math.min(delay * 1.5, 10000); // Exponential backoff, max 10 seconds
      }
    }
  }

  throw new Error(`Database not ready after ${maxAttempts} attempts. Last error: ${lastError.message}`);
}

async function createOrUpdateSecret(client, secretName, secretValue, description) {
  let arn;
  try {
    const result = await client.send(new UpdateSecretCommand({
      SecretId: secretName,
      SecretString: secretValue,
      Description: description,
    }));
    arn = result.ARN;
    console.log(`Updated secret: ${secretName}, ARN: ${arn}`);
  } catch (e) {
    if (e.name === "ResourceNotFoundException") {
      const result = await client.send(new CreateSecretCommand({
        Name: secretName,
        SecretString: secretValue,
        Description: description,
      }));
      arn = result.ARN;
      console.log(`Created secret: ${secretName}, ARN: ${arn}`);
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
  console.log(`Created/updated SSM param: ${paramName}`);
}

async function ensureDatabaseExists(host, port, username, password, databaseName) {
  // Connect to the default 'postgres' database to create the service database
  const adminClient = new Client({
    host,
    port,
    user: username,
    password,
    database: "postgres",
    ssl: { rejectUnauthorized: false },
  });

  try {
    await adminClient.connect();
    console.log(`Connected to postgres admin database on ${host}`);

    // Check if database exists
    const checkResult = await adminClient.query(
      "SELECT 1 FROM pg_database WHERE datname = $1",
      [databaseName]
    );

    if (checkResult.rows.length === 0) {
      // Database doesn't exist, create it
      // Note: CREATE DATABASE cannot be parameterized, so we sanitize the name
      const safeDatabaseName = databaseName.replace(/[^a-zA-Z0-9_]/g, "_");
      await adminClient.query(`CREATE DATABASE "${safeDatabaseName}"`);
      console.log(`Created database: ${safeDatabaseName}`);
    } else {
      console.log(`Database ${databaseName} already exists`);
    }
  } finally {
    await adminClient.end();
  }
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
      ? [`${secretNamePrefix}/database-host`, `${secretNamePrefix}/database-user`, `${secretNamePrefix}/database-password`]
      : [`${secretNamePrefix}/connection-string`];

    for (const secretName of secretsToDelete) {
      try {
        await smClient.send(new DeleteSecretCommand({ SecretId: secretName, ForceDeleteWithoutRecovery: true }));
      } catch (e) {
        if (e.name !== "ResourceNotFoundException") console.warn(`Failed to delete secret ${secretName}:`, e);
      }
    }

    // Clean up SSM parameters
    const paramsToDelete = format === "individual_fields"
      ? [`${ssmBasePath}/database-host`, `${ssmBasePath}/database-user`, `${ssmBasePath}/database-password`]
      : [`${ssmBasePath}/connection_string`];

    for (const paramName of paramsToDelete) {
      try {
        await ssmClient.send(new DeleteParameterCommand({ Name: paramName }));
      } catch (e) {
        if (e.name !== "ParameterNotFound") console.warn(`Failed to delete param ${paramName}:`, e);
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

  // Wait for the database to be ready (important for newly created clusters)
  console.log(`Waiting for database at ${host}:${port} to be ready...`);
  await waitForDatabaseReady(host, port, username, password);

  // Create the database if it doesn't exist
  await ensureDatabaseExists(host, port, username, password, databaseName);

  let connectionStringArn = null;
  let connectionStringValue = null;

  if (format === "dotnet") {
    // Escape single quotes and wrap password in quotes to handle special characters (; = etc.)
    // Semi-colons are used as delimiters in .NET connection strings, so they must be escaped.
    const escapedPassword = password.replace(/'/g, "''");
    const connStr = `Host=${host};Database=${databaseName};Username=${username};Password='${escapedPassword}'`;
    connectionStringValue = connStr;
    const [arn] = await Promise.all([
      createOrUpdateSecret(smClient, `${secretNamePrefix}/connection-string`, connStr, `Database connection string (auto-generated)`),
      createOrUpdateSsmParam(ssmClient, `${ssmBasePath}/connection_string`, connStr, `Database connection string (auto-generated)`),
    ]);
    connectionStringArn = arn;
  } else if (format === "postgres_url") {
    const encodedPassword = encodeURIComponent(password);
    const connStr = `postgres://${username}:${encodedPassword}@${host}:${port}/${databaseName}?sslmode=require`;
    connectionStringValue = connStr;
    const [arn] = await Promise.all([
      createOrUpdateSecret(smClient, `${secretNamePrefix}/connection-string`, connStr, `Database connection string (auto-generated)`),
      createOrUpdateSsmParam(ssmClient, `${ssmBasePath}/connection_string`, connStr, `Database connection string (auto-generated)`),
    ]);
    connectionStringArn = arn;
  } else if (format === "individual_fields") {
    const jdbcUrl = `jdbc:postgresql://${host}:${port}/${databaseName}`;
    await Promise.all([
      createOrUpdateSecret(smClient, `${secretNamePrefix}/database-host`, jdbcUrl, `JDBC URL (auto-generated)`),
      createOrUpdateSecret(smClient, `${secretNamePrefix}/database-user`, username, `Database username (auto-generated)`),
      createOrUpdateSecret(smClient, `${secretNamePrefix}/database-password`, password, `Database password (auto-generated)`),
      createOrUpdateSsmParam(ssmClient, `${ssmBasePath}/database-host`, jdbcUrl, `JDBC URL (auto-generated)`),
      createOrUpdateSsmParam(ssmClient, `${ssmBasePath}/database-user`, username, `Database username (auto-generated)`),
      createOrUpdateSsmParam(ssmClient, `${ssmBasePath}/database-password`, password, `Database password (auto-generated)`),
    ]);
  }

  return {
    PhysicalResourceId: `${secretNamePrefix}-db-creds`,
    Data: {
      SecretNamePrefix: secretNamePrefix,
      ConnectionStringArn: connectionStringArn,
      ConnectionStringValue: connectionStringValue,
      DatabaseName: databaseName,
    }
  };
};
