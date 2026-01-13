/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import { Construct, IDependable } from "constructs";
import { CustomResource, Duration, RemovalPolicy, Stack } from "aws-cdk-lib";
import * as ec2 from "aws-cdk-lib/aws-ec2";
import * as ecs from "aws-cdk-lib/aws-ecs";
import * as iam from "aws-cdk-lib/aws-iam";
import { Function as LambdaFunction, Runtime, Code } from "aws-cdk-lib/aws-lambda";
import { Provider } from "aws-cdk-lib/custom-resources";
import { Platform } from "aws-cdk-lib/aws-ecr-assets";
import { SharedProps } from "./shared-props";

export interface MigrationTaskProps {
  readonly sharedProps: SharedProps;
  readonly vpc: ec2.IVpc;
  readonly cluster: ecs.ICluster;
  /** Container image registry path (e.g., "ghcr.io/datadog/stickerlandia/user-management-migration") */
  readonly image: string;
  /** Image tag (e.g., "latest" or commit SHA) */
  readonly imageTag: string;
  /** Path to build directory for local builds (when imageTag === "LOCAL") */
  readonly assetPath?: string;
  /** Path to Dockerfile (if not standard "Dockerfile" in assetPath) */
  readonly dockerfile?: string;
  /** Entry point for the container (overrides Dockerfile ENTRYPOINT) */
  readonly entryPoint?: string[];
  /** Command to run in the container (overrides Dockerfile CMD) */
  readonly command?: string[];
  /** Environment variables for the migration container */
  readonly environmentVariables: { [key: string]: string };
  /** Secrets for the migration container */
  readonly secrets: { [key: string]: ecs.Secret };
  /** Security group ID to use (if not provided, creates a new one) */
  readonly securityGroupId?: string;
  /** Subnet IDs to deploy into (if not provided, auto-selects based on deployInPrivateSubnet) */
  readonly subnetIds?: string[];
  /** Deploy in private subnet (default: true) - only used if subnetIds not provided */
  readonly deployInPrivateSubnet?: boolean;
  /** Timeout for task completion in seconds (default: 300) */
  readonly timeout?: number;
  /** Resources that must be created before migration runs */
  readonly dependencies?: IDependable[];
  /** Runtime platform for the container (default: ARM64 Linux) */
  readonly runtimePlatform?: ecs.RuntimePlatform;
}

/**
 * A construct that runs a one-shot ECS Fargate task during CloudFormation deployment.
 *
 * This is useful for database migrations, seeding, and other initialization tasks
 * that need to run before services start.
 *
 * The task runs on CREATE and UPDATE events, and the construct waits for the task
 * to complete before allowing dependent resources to proceed.
 */
export class MigrationTask extends Construct implements IDependable {
  /** The custom resource - other constructs can depend on this */
  public readonly resource: CustomResource;
  public readonly taskDefinition: ecs.FargateTaskDefinition;
  public readonly taskRole: iam.IRole;
  public readonly executionRole: iam.IRole;

  constructor(scope: Construct, id: string, props: MigrationTaskProps) {
    super(scope, id);

    const region = Stack.of(this).region;
    const account = Stack.of(this).account;
    const timeout = props.timeout ?? 300;
    const deployInPrivateSubnet = props.deployInPrivateSubnet ?? true;

    // Execution Role - needed to pull images and write logs
    this.executionRole = new iam.Role(this, "ExecutionRole", {
      assumedBy: new iam.ServicePrincipal("ecs-tasks.amazonaws.com"),
    });
    this.executionRole.addManagedPolicy(
      iam.ManagedPolicy.fromManagedPolicyArn(
        this,
        "TaskExecutionPolicy",
        "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
      )
    );

    // Task Role - for the application to access AWS services
    this.taskRole = new iam.Role(this, "TaskRole", {
      assumedBy: new iam.ServicePrincipal("ecs-tasks.amazonaws.com"),
    });

    // Task Definition - use Datadog wrapper when enabled (same pattern as WebService)
    const runtimePlatform = props.runtimePlatform ?? {
      cpuArchitecture: ecs.CpuArchitecture.ARM64,
      operatingSystemFamily: ecs.OperatingSystemFamily.LINUX,
    };

    if (!props.sharedProps.enableDatadog) {
      this.taskDefinition = new ecs.FargateTaskDefinition(this, "TaskDefinition", {
        memoryLimitMiB: 512,
        cpu: 256,
        runtimePlatform,
        executionRole: this.executionRole,
        taskRole: this.taskRole,
      });
    } else {
      this.taskDefinition = props.sharedProps.datadog.ecsFargate.fargateTaskDefinition(
        this,
        "TaskDefinition",
        {
          memoryLimitMiB: 512,
          cpu: 256,
          runtimePlatform,
          executionRole: this.executionRole,
          taskRole: this.taskRole,
        }
      );
    }

    // Container image - support both registry and local builds
    const isLocalBuild = props.imageTag.toUpperCase() === "LOCAL" && props.assetPath;
    const assetPlatform = runtimePlatform.cpuArchitecture === ecs.CpuArchitecture.ARM64
      ? Platform.LINUX_ARM64
      : Platform.LINUX_AMD64;
    const containerImage = isLocalBuild
      ? ecs.ContainerImage.fromAsset(props.assetPath!, {
          exclude: ["infra", "cdk.out", "node_modules", ".git"],
          platform: assetPlatform,
          file: props.dockerfile,
        })
      : ecs.ContainerImage.fromRegistry(`${props.image}:${props.imageTag}`);

    // Base environment variables (same pattern as WebService)
    const baseEnvironmentVariables: { [key: string]: string } = {
      ENV: props.sharedProps.environment,
      DD_ENV: props.sharedProps.environment,
      DD_SERVICE: `${props.sharedProps.serviceName}-migration`,
      DD_VERSION: props.imageTag,
      DD_GIT_COMMIT_SHA: props.imageTag,
      DD_GIT_REPOSITORY_URL: "https://github.com/Datadog/stickerlandia",
    };

    // Add container to task definition
    const container = this.taskDefinition.addContainer("migration", {
      image: containerImage,
      containerName: "migration",
      environment: {
        ...baseEnvironmentVariables,
        ...props.environmentVariables,
      },
      secrets: props.secrets,
      entryPoint: props.entryPoint,
      command: props.command,
      essential: true,
    });

    // Security group for the migration task - use provided or create new
    const securityGroup = props.securityGroupId
      ? ec2.SecurityGroup.fromSecurityGroupId(this, "SecurityGroup", props.securityGroupId)
      : new ec2.SecurityGroup(this, "SecurityGroup", {
          vpc: props.vpc,
          description: "Security group for migration task",
          allowAllOutbound: true,
        });

    // Lambda function to run the ECS task and wait for completion
    const handler = new LambdaFunction(this, "Handler", {
      runtime: Runtime.NODEJS_20_X,
      handler: "index.handler",
      timeout: Duration.seconds(Math.min(timeout + 30, 900)), // Lambda max is 15 minutes
      code: Code.fromInline(`
const { ECSClient, RunTaskCommand, DescribeTasksCommand, waitUntilTasksStopped } = require("@aws-sdk/client-ecs");

exports.handler = async (event) => {
  console.log("Event:", JSON.stringify(event, null, 2));

  const props = event.ResourceProperties;
  const client = new ECSClient({});

  // On Delete, we don't need to do anything - migrations are forward-only
  if (event.RequestType === "Delete") {
    return { PhysicalResourceId: event.PhysicalResourceId };
  }

  // Run the migration task
  console.log("Starting migration task...");
  const runTaskResult = await client.send(new RunTaskCommand({
    cluster: props.ClusterArn,
    taskDefinition: props.TaskDefinitionArn,
    launchType: "FARGATE",
    networkConfiguration: {
      awsvpcConfiguration: {
        assignPublicIp: props.AssignPublicIp,
        subnets: props.Subnets,
        securityGroups: props.SecurityGroups,
      },
    },
    platformVersion: "LATEST",
  }));

  if (!runTaskResult.tasks || runTaskResult.tasks.length === 0) {
    const failures = runTaskResult.failures || [];
    throw new Error(\`Failed to start migration task: \${JSON.stringify(failures)}\`);
  }

  const taskArn = runTaskResult.tasks[0].taskArn;
  console.log(\`Migration task started: \${taskArn}\`);

  // Wait for the task to complete
  console.log("Waiting for migration task to complete...");
  try {
    await waitUntilTasksStopped(
      { client, maxWaitTime: ${timeout}, minDelay: 5, maxDelay: 10 },
      { cluster: props.ClusterArn, tasks: [taskArn] }
    );
  } catch (e) {
    throw new Error(\`Migration task timed out or failed to complete: \${e.message}\`);
  }

  // Check the task exit code
  const describeResult = await client.send(new DescribeTasksCommand({
    cluster: props.ClusterArn,
    tasks: [taskArn],
  }));

  const task = describeResult.tasks[0];
  const container = task.containers.find(c => c.name === "migration");

  if (!container) {
    throw new Error("Migration container not found in task");
  }

  console.log(\`Migration task stopped. Exit code: \${container.exitCode}, Reason: \${task.stoppedReason || "N/A"}\`);

  if (container.exitCode !== 0) {
    throw new Error(\`Migration failed with exit code \${container.exitCode}. Reason: \${task.stoppedReason || "Unknown"}\`);
  }

  console.log("Migration completed successfully!");

  return {
    PhysicalResourceId: \`migration-\${Date.now()}\`,
    Data: {
      TaskArn: taskArn,
      ExitCode: container.exitCode,
    },
  };
};
      `),
    });

    // Grant Lambda permission to run ECS tasks
    handler.addToRolePolicy(
      new iam.PolicyStatement({
        effect: iam.Effect.ALLOW,
        actions: ["ecs:RunTask"],
        resources: [this.taskDefinition.taskDefinitionArn],
      })
    );

    // Grant Lambda permission to describe tasks (for waiting)
    handler.addToRolePolicy(
      new iam.PolicyStatement({
        effect: iam.Effect.ALLOW,
        actions: ["ecs:DescribeTasks"],
        resources: ["*"], // DescribeTasks doesn't support resource-level permissions
        conditions: {
          ArnEquals: {
            "ecs:cluster": props.cluster.clusterArn,
          },
        },
      })
    );

    // Grant Lambda permission to pass roles to ECS
    handler.addToRolePolicy(
      new iam.PolicyStatement({
        effect: iam.Effect.ALLOW,
        actions: ["iam:PassRole"],
        resources: [this.taskRole.roleArn, this.executionRole.roleArn],
      })
    );

    // Create the provider
    const provider = new Provider(this, "Provider", {
      onEventHandler: handler,
    });

    // Determine subnets - use provided or auto-select
    const subnets = props.subnetIds ?? (deployInPrivateSubnet
      ? props.vpc.selectSubnets({ subnetType: ec2.SubnetType.PRIVATE_WITH_EGRESS }).subnetIds
      : props.vpc.selectSubnets({ subnetType: ec2.SubnetType.PUBLIC }).subnetIds);

    // Create the custom resource
    this.resource = new CustomResource(this, "Resource", {
      serviceToken: provider.serviceToken,
      properties: {
        ClusterArn: props.cluster.clusterArn,
        TaskDefinitionArn: this.taskDefinition.taskDefinitionArn,
        Subnets: subnets,
        SecurityGroups: [securityGroup.securityGroupId],
        AssignPublicIp: deployInPrivateSubnet ? "DISABLED" : "ENABLED",
        // Force re-run on every deployment by including a timestamp
        // Remove this if you want migrations to only run when task definition changes
        Timestamp: Date.now().toString(),
      },
      removalPolicy: RemovalPolicy.DESTROY,
    });

    // Add dependencies
    if (props.dependencies) {
      for (const dep of props.dependencies) {
        this.resource.node.addDependency(dep);
      }
    }
  }
}
