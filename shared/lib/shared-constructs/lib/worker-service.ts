/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import * as cdk from "aws-cdk-lib";
import { Tags } from "aws-cdk-lib";
import * as ec2 from "aws-cdk-lib/aws-ec2";
import * as ecs from "aws-cdk-lib/aws-ecs";
import * as iam from "aws-cdk-lib/aws-iam";
import * as servicediscovery from "aws-cdk-lib/aws-servicediscovery";
import * as ssm from "aws-cdk-lib/aws-ssm";
import { Construct, IConstruct, IDependable } from "constructs";
import { SharedProps } from "./shared-props";

export interface WorkerServiceProps {
  readonly sharedProps: SharedProps;
  readonly vpc: ec2.IVpc;
  readonly cluster: ecs.ICluster;
  readonly image: string;
  readonly imageTag: string;
  readonly ddApiKey: ssm.IStringParameter;
  readonly environmentVariables: { [key: string]: string };
  readonly secrets: { [key: string]: ecs.Secret };
  readonly serviceDiscoveryNamespace: servicediscovery.IPrivateDnsNamespace;
  readonly serviceDiscoveryName: string;
  readonly runtimePlatform: ecs.RuntimePlatform;
  readonly deployInPrivateSubnet?: boolean;
  readonly healthCheckCommand?: ecs.HealthCheck;
  /** Resources that must be created before the ECS service starts (e.g., SSM parameters from custom resources) */
  readonly serviceDependencies?: IDependable[];
}

export class WorkerService extends Construct {
  public readonly executionRole: iam.IRole;
  public readonly taskRole: iam.IRole;
  public readonly cloudMapService: servicediscovery.Service;

  constructor(scope: Construct, id: string, props: WorkerServiceProps) {
    super(scope, id);
    // Execution Role
    this.executionRole = new iam.Role(
      this,
      `${props.sharedProps.serviceName}ExecutionRole`,
      {
        assumedBy: new iam.ServicePrincipal("ecs-tasks.amazonaws.com"),
      }
    );

    this.executionRole.addManagedPolicy(
      iam.ManagedPolicy.fromManagedPolicyArn(
        this,
        "TaskExecutionPolicy",
        "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
      )
    );

    // Task Role
    this.taskRole = new iam.Role(
      this,
      `${props.sharedProps.serviceName}TaskRole`,
      {
        assumedBy: new iam.ServicePrincipal("ecs-tasks.amazonaws.com"),
      }
    );

    // Base environment variables
    const baseEnvironmentVariables: { [key: string]: string } = {
      ENV: props.sharedProps.environment,
      DD_ENV: props.sharedProps.environment,
      DD_SERVICE: props.sharedProps.serviceName,
      DD_VERSION: props.imageTag,
      DD_GIT_COMMIT_SHA: props.imageTag,
      DD_GIT_REPOSITORY_URL: "https://github.com/Datadog/stickerlandia",
      DD_AGENT_HOST: "127.0.0.1",
      DD_TRACE_ROUTE_TEMPLATE_RESOURCE_NAMES_ENABLED: "true",
      DD_RUNTIME_METRICS_ENABLED: "true",
      DD_PROFILING_ENABLED: "true",
      DD_LOGS_INJECTION: "true",
      DD_IAST_ENABLED: "true",
      ECS_ENABLE_CONTAINER_METADATA: "true",
    };

    // Merge environment variables and secrets
    const finalEnvironmentVariables = {
      ...baseEnvironmentVariables,
      ...props.environmentVariables,
    };

    let taskDefinition: ecs.FargateTaskDefinition | undefined = undefined;

    // Task Definition
    if (!props.sharedProps.enableDatadog) {
      taskDefinition = new ecs.FargateTaskDefinition(
        this,
        `${props.sharedProps.serviceName}WorkerDefinition`,
        {
          memoryLimitMiB: 512,
          runtimePlatform: props.runtimePlatform,
          executionRole: this.executionRole,
          taskRole: this.taskRole,
        }
      );
    } else {
      taskDefinition =
        props.sharedProps.datadog.ecsFargate.fargateTaskDefinition(
          this,
          `${props.sharedProps.serviceName}WorkerDefinition`,
          {
            memoryLimitMiB: 512,
            runtimePlatform: props.runtimePlatform,
            executionRole: this.executionRole,
            taskRole: this.taskRole,
          }
        );
    }

    // Application Container
    const applicationContainer = taskDefinition!.addContainer("application", {
      image: ecs.ContainerImage.fromRegistry(
        `${props.image}:${props.imageTag}`
      ),
      portMappings: [],
      containerName: props.sharedProps.serviceName,
      environment: finalEnvironmentVariables,
      secrets: props.secrets,
      healthCheck: props.healthCheckCommand,
    });

    // Fargate Service
    const service = new ecs.FargateService(
      this,
      `${props.sharedProps.serviceName}WorkerService`,
      {
        cluster: props.cluster,
        taskDefinition,
        desiredCount: 1,
        assignPublicIp: !props.deployInPrivateSubnet,
        vpcSubnets: {
          subnets: props.deployInPrivateSubnet
            ? props.vpc.privateSubnets
            : props.vpc.publicSubnets,
        },
        capacityProviderStrategies: [
          {
            capacityProvider:
              props.sharedProps.environment !== "prod"
                ? "FARGATE_SPOT"
                : "FARGATE",
            weight: 1,
          },
        ],
      }
    );

    // Add explicit CloudFormation dependencies for resources that must exist before ECS tasks start
    // (e.g., SSM parameters created by custom resources)
    if (props.serviceDependencies) {
      for (const dependency of props.serviceDependencies) {
        service.node.addDependency(dependency);
      }
    }

    // Add dependency on cluster capacity provider associations if they exist
    // This ensures proper deletion order: service -> associations -> cluster
    // CDK creates CfnClusterCapacityProviderAssociations lazily during synthesis,
    // so we use Aspects to add the dependency after all constructs are created
    // See: https://github.com/aws/aws-cdk/issues/19275
    cdk.Aspects.of(service).add({
      visit: (node: IConstruct) => {
        if (node === service) {
          const children = props.cluster.node.findAll();
          for (const child of children) {
            // Use constructor name check to avoid instanceof issues with multiple module instances
            if (child.constructor.name === 'CfnClusterCapacityProviderAssociations') {
              // Service depends on associations (service deleted before associations)
              node.node.addDependency(child);
              // Associations depend on cluster (associations deleted before cluster)
              child.node.addDependency(props.cluster);
            }
          }
        }
      }
    });

    // Add tags
    Tags.of(service).add("service", props.sharedProps.serviceName);
    Tags.of(service).add("commitHash", props.imageTag);

    // Grant permissions
    props.ddApiKey.grantRead(this.executionRole);
  }
}
