/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import { Duration, Tags } from "aws-cdk-lib";
import * as apigatewayv2 from "aws-cdk-lib/aws-apigatewayv2";
import * as integrations from "aws-cdk-lib/aws-apigatewayv2-integrations";
import * as ec2 from "aws-cdk-lib/aws-ec2";
import * as ecs from "aws-cdk-lib/aws-ecs";
import {
  ApplicationTargetGroup,
  IApplicationListener,
  IApplicationLoadBalancer,
  ListenerCondition,
} from "aws-cdk-lib/aws-elasticloadbalancingv2";
import * as iam from "aws-cdk-lib/aws-iam";
import * as servicediscovery from "aws-cdk-lib/aws-servicediscovery";
import * as ssm from "aws-cdk-lib/aws-ssm";
import { Construct } from "constructs";
import { SharedProps } from "./shared-props";

export interface WebServiceProps {
  readonly sharedProps: SharedProps;
  readonly vpc: ec2.IVpc;
  readonly vpcLink: apigatewayv2.IVpcLink;
  readonly vpcLinkSecurityGroupId: string;
  readonly httpApi: apigatewayv2.IHttpApi;
  readonly applicationLoadBalancer: IApplicationLoadBalancer;
  readonly applicationListener: IApplicationListener;
  readonly cluster: ecs.ICluster;
  readonly image: string;
  readonly imageTag: string;
  readonly ddApiKey: ssm.IStringParameter;
  readonly port: number;
  readonly environmentVariables: { [key: string]: string };
  readonly secrets: { [key: string]: ecs.Secret };
  readonly path: string;
  readonly healthCheckPath: string;
  readonly serviceDiscoveryNamespace: servicediscovery.IPrivateDnsNamespace;
  readonly serviceDiscoveryName: string;
  readonly deployInPrivateSubnet?: boolean;
}

export class WebService extends Construct {
  public readonly executionRole: iam.IRole;
  public readonly taskRole: iam.IRole;
  public readonly cloudMapService: servicediscovery.Service;

  constructor(scope: Construct, id: string, props: WebServiceProps) {
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

    // Task Definition
    const taskDefinition =
      props.sharedProps.datadog.ecsFargate.fargateTaskDefinition(
        this,
        `${props.sharedProps.serviceName}Definition`,
        {
          memoryLimitMiB: 512,
          runtimePlatform: {
            cpuArchitecture: ecs.CpuArchitecture.X86_64,
            operatingSystemFamily: ecs.OperatingSystemFamily.LINUX,
          },
          executionRole: this.executionRole,
          taskRole: this.taskRole,
        }
      );

    // Application Container
    const container = taskDefinition.addContainer("application", {
      image: ecs.ContainerImage.fromRegistry(
        `${props.image}:${props.imageTag}`
      ),
      portMappings: [
        {
          containerPort: props.port,
          protocol: ecs.Protocol.TCP,
        },
      ],
      //TODO: Add health checks
      containerName: props.sharedProps.serviceName,
      environment: finalEnvironmentVariables,
      // logging: ecs.LogDrivers.firelens({
      //   options: {
      //     Name: "datadog",
      //     Host: "http-intake.logs.datadoghq.eu",
      //     TLS: "on",
      //     dd_service: props.sharedProps.serviceName,
      //     dd_source: "aspnet",
      //     dd_message_key: "log",
      //     dd_tags: `project:${props.sharedProps.serviceName}`,
      //     provider: "ecs",
      //   },
      //   secretOptions: {
      //     apikey: ecs.Secret.fromSsmParameter(props.ddApiKey),
      //   },
      // }),
    });

    // // Add Docker labels
    // container.addDockerLabel("com.datadoghq.tags.env", props.environment);
    // container.addDockerLabel("com.datadoghq.tags.service", props.serviceName);
    // container.addDockerLabel("com.datadoghq.tags.version", props.imageTag);

    // // DataDog Agent Container
    // taskDefinition.addContainer("datadog-agent", {
    //   image: ecs.ContainerImage.fromRegistry(
    //     "public.ecr.aws/datadog/agent:latest"
    //   ),
    //   portMappings: [
    //     {
    //       containerPort: 4317,
    //     },
    //     {
    //       containerPort: 4318,
    //     },
    //     {
    //       containerPort: 8126,
    //     },
    //   ],
    //   containerName: "datadog-agent",
    //   environment: {
    //     DD_SITE: "datadoghq.eu",
    //     ECS_FARGATE: "true",
    //     DD_LOGS_ENABLED: "false",
    //     DD_PROCESS_AGENT_ENABLED: "true",
    //     DD_APM_ENABLED: "true",
    //     DD_APM_NON_LOCAL_TRAFFIC: "true",
    //     DD_DOGSTATSD_NON_LOCAL_TRAFFIC: "true",
    //     DD_ENV: props.environment,
    //     DD_SERVICE: props.serviceName,
    //     DD_VERSION: props.imageTag,
    //     DD_APM_IGNORE_RESOURCES: `(GET) ${props.healthCheckPath}`,
    //   },
    //   secrets: {
    //     DD_API_KEY: ecs.Secret.fromSsmParameter(props.ddApiKey),
    //   },
    // });

    // // Firelens Log Router
    // taskDefinition.addFirelensLogRouter("firelens", {
    //   essential: true,
    //   image: ecs.ContainerImage.fromRegistry(
    //     "amazon/aws-for-fluent-bit:stable"
    //   ),
    //   containerName: "log-router",
    //   firelensConfig: {
    //     type: ecs.FirelensLogRouterType.FLUENTBIT,
    //     options: {
    //       enableECSLogMetadata: true,
    //     },
    //   },
    // });

    // Cloud Map Service
    this.cloudMapService = new servicediscovery.Service(
      this,
      "CloudMapService",
      {
        namespace: props.serviceDiscoveryNamespace,
        name: props.serviceDiscoveryName,
        dnsTtl: Duration.seconds(60),
        dnsRecordType: servicediscovery.DnsRecordType.SRV,
      }
    );

    // Fargate Service
    const service = new ecs.FargateService(
      this,
      `${props.sharedProps.serviceName}Service`,
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
      }
    );

    // Associate with Cloud Map
    service.associateCloudMapService({
      service: this.cloudMapService,
    });

    // Add security group ingress rules
    for (const securityGroup of service.connections.securityGroups) {
      securityGroup.addIngressRule(
        ec2.Peer.securityGroupId(props.vpcLinkSecurityGroupId),
        ec2.Port.tcp(props.port)
      );
    }

    // Add tags
    Tags.of(service).add("service", props.sharedProps.serviceName);
    Tags.of(service).add("commitHash", props.imageTag);

    // Grant permissions
    props.ddApiKey.grantRead(this.executionRole);

    // API Gateway Integration
    const serviceDiscoveryIntegration =
      new integrations.HttpServiceDiscoveryIntegration(
        "ApplicationServiceDiscovery",
        this.cloudMapService,
        {
          method: apigatewayv2.HttpMethod.ANY,
          vpcLink: props.vpcLink,
        }
      );

    // const targetGroup = new ApplicationTargetGroup(this, id + 'target-group', {
    //   targetGroupName: props.serviceName + '-TG-' + props.environment,
    //   vpc: props.vpc,
    //   port: 80,
    //   healthCheck: {
    //     healthyHttpCodes: '200-499',
    //     path: props.healthCheckPath ?? '/',
    //     port: props.port.toString(),
    //   }
    // });

    // props.applicationListener.addTargetGroups(id + '-listener-target', {
    //   targetGroups: [targetGroup],
    //   priority: 10,
    //   conditions: [
    //     ListenerCondition.pathPatterns(['/api/users/v1/*'])
    //   ]
    // });

    // targetGroup.addTarget(service);

    // HTTP Route
    new apigatewayv2.HttpRoute(this, "ProxyRoute", {
      httpApi: props.httpApi,
      routeKey: apigatewayv2.HttpRouteKey.with(
        props.path,
        apigatewayv2.HttpMethod.ANY
      ),
      integration: serviceDiscoveryIntegration,
    });
  }
}
