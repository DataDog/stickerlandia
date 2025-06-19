import { Vpc, SecurityGroup, Port } from "aws-cdk-lib/aws-ec2";
import { Construct } from "constructs";
import { SharedProps } from "./constructs/shared-props";
import {
  Cluster,
  ContainerImage,
  CpuArchitecture,
  FargateTaskDefinition,
  FirelensLogRouterType,
  LogDrivers,
  OperatingSystemFamily,
  Protocol,
} from "aws-cdk-lib/aws-ecs";
import { Repository } from "aws-cdk-lib/aws-ecr";
import { Topic } from "aws-cdk-lib/aws-sns";
import { Queue } from "aws-cdk-lib/aws-sqs";
import {
  ApplicationProtocol,
  Protocol as HealthCheckProtocol,
} from "aws-cdk-lib/aws-elasticloadbalancingv2";
import { Duration } from "aws-cdk-lib";
import { ApplicationLoadBalancedFargateService } from "aws-cdk-lib/aws-ecs-patterns";

export class ApiProps {
  sharedProps: SharedProps;
  vpc: Vpc;
  cluster: Cluster;
}

export class Api extends Construct {
  stickerClaimedQueue: Queue;
  stickerClaimedDLQ: Queue;
  userRegisteredTopic: Topic;
  constructor(scope: Construct, id: string, props: ApiProps) {
    super(scope, id);

    const ecrRepository = Repository.fromRepositoryName(
      this,
      "UserServiceRepo",
      "stickerlandia-user-management"
    );

    this.userRegisteredTopic = new Topic(this, "UserRegisteredTopic", {
      topicName: `${props.sharedProps.serviceName}-${props.sharedProps.environment}-user-registered`,
    });
    this.stickerClaimedDLQ = new Queue(this, "StickerClaimedDLQ", {
      queueName: `${props.sharedProps.serviceName}-${props.sharedProps.environment}-sticker-claimed-dlq`,
    });

    //TODO: Add EventBridge rule mapping to subscribe to sticker claimed events published to the shared EventBus.
    this.stickerClaimedQueue = new Queue(this, "StickerClaimedQueue", {
      queueName: `${props.sharedProps.serviceName}-${props.sharedProps.environment}-sticker-claimed`,
      deadLetterQueue: {
        queue: this.stickerClaimedDLQ,
        maxReceiveCount: 5, // Messages will be sent to DLQ after 5 failed attempts
      },
    });

    const applicationTaskDef = new FargateTaskDefinition(
      this,
      "UserServiceTaskDef",
      {
        runtimePlatform: {
          operatingSystemFamily: OperatingSystemFamily.LINUX,
          cpuArchitecture: CpuArchitecture.ARM64,
        },
        cpu: 256,
        memoryLimitMiB: 512,
      }
    );

    const containerPort = 8080;

    const container = applicationTaskDef.addContainer("UserServiceContainer", {
      image: ContainerImage.fromEcrRepository(
        ecrRepository,
        props.sharedProps.version
      ),
      portMappings: [
        {
          containerPort: containerPort,
          protocol: Protocol.TCP,
        },
      ],
      environment: {
        ConnectionStrings__messaging: "",
        ConnectionStrings__database: props.sharedProps.connectionString,
        Aws__UserRegisteredTopicArn: this.userRegisteredTopic.topicArn,
        Aws__StickerClaimedQueueUrl: this.stickerClaimedQueue.queueUrl,
        Aws__StickerClaimedDLQUrl: this.stickerClaimedDLQ.queueUrl,
        DRIVING: "ASPNET",
        DRIVEN: "AWS",
        DISABLE_SSL: "true",
      },
      logging: LogDrivers.firelens({
        options: {
          Name: "datadog",
          Host: "http-intake.logs.datadoghq.eu",
          TLS: "on",
          dd_service: props.sharedProps.serviceName,
          dd_source: "aspnet",
          dd_message_key: "log",
          dd_tags: `project:${props.sharedProps.serviceName}`,
          provider: "ecs",
          apikey: props.sharedProps.datadog.apiKey,
        },
      }),
    });
    container.addDockerLabel(
      "com.datadoghq.tags.env",
      props.sharedProps.environment
    );
    container.addDockerLabel(
      "com.datadoghq.tags.service",
      props.sharedProps.serviceName
    );
    container.addDockerLabel(
      "com.datadoghq.tags.version",
      props.sharedProps.version
    );

    container.addPortMappings({
      containerPort: containerPort,
      protocol: Protocol.TCP,
    });

    applicationTaskDef.addContainer("datadog-agent", {
      image: ContainerImage.fromRegistry("public.ecr.aws/datadog/agent:latest"),
      portMappings: [
        {
          containerPort: 8125,
          protocol: Protocol.UDP, // Dogstatsd port
        },
        {
          containerPort: 8126,
          protocol: Protocol.TCP, // APM port
        },
      ],
      containerName: "datadog-agent",
      environment: {
        DD_API_KEY: props.sharedProps.datadog.apiKey,
        DD_SITE: props.sharedProps.datadog.site,
        DD_APM_ENABLED: "true",
        DD_LOGS_ENABLED: "true",
        ECS_FARGATE: "true",
        DD_APM_NON_LOCAL_TRAFFIC: "true",
        DD_DOGSTATSD_NON_LOCAL_TRAFFIC: "true",
        DD_APM_IGNORE_RESOURCES: `(GET) /api/users/v1/health`,
      },
    });
    applicationTaskDef.addFirelensLogRouter("firelens-router", {
      essential: true,
      image: ContainerImage.fromRegistry("amazon/aws-for-fluent-bit:stable"),
      firelensConfig: {
        type: FirelensLogRouterType.FLUENTBIT,
        options: {
          enableECSLogMetadata: true,
        },
      },
    });

    this.userRegisteredTopic.grantPublish(applicationTaskDef.taskRole);
    this.stickerClaimedQueue.grantSendMessages(applicationTaskDef.taskRole);
    this.stickerClaimedDLQ.grantSendMessages(applicationTaskDef.taskRole);
    this.stickerClaimedQueue.grantConsumeMessages(applicationTaskDef.taskRole);
    this.stickerClaimedDLQ.grantConsumeMessages(applicationTaskDef.taskRole);

    // TODO: move this to a shared infra project to allow one ALB across multiple services
    const service = new ApplicationLoadBalancedFargateService(
      this,
      "UserServiceFargateService",
      {
        cluster: props.cluster,
        taskDefinition: applicationTaskDef,
        assignPublicIp: false,
        publicLoadBalancer: true,
        desiredCount: 1,
        loadBalancerName: `${props.sharedProps.serviceName}-${props.sharedProps.environment}-user-service-lb`,
        serviceName: `${props.sharedProps.serviceName}-${props.sharedProps.environment}-user-service`,
        listenerPort: 80,
        protocol: ApplicationProtocol.HTTP,
        healthCheckGracePeriod: Duration.minutes(2),
        circuitBreaker: {
          rollback: true,
          enable: true,
        },
      }
    );
    service.targetGroup.configureHealthCheck({
      path: "/api/users/v1/health",
      interval: Duration.seconds(60),
      timeout: Duration.seconds(5),
      enabled: true,
      healthyHttpCodes: "200-499",
    });
  }
}
