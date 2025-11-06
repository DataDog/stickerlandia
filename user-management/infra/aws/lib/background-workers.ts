/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import { Construct } from "constructs";
import { SharedProps } from "../../../../shared/lib/shared-constructs/lib/shared-props";
import { InstrumentedLambdaFunction } from "./constructs/instrumented-function";
import { Duration } from "aws-cdk-lib";
import { IQueue } from "aws-cdk-lib/aws-sqs";
import { SqsEventSource } from "aws-cdk-lib/aws-lambda-event-sources";
import { SqsDestination } from "aws-cdk-lib/aws-lambda-destinations";
import {
  IEventBus,
  Rule,
  RuleTargetInput,
  Schedule,
} from "aws-cdk-lib/aws-events";
import { LambdaFunction, SqsQueue } from "aws-cdk-lib/aws-events-targets";
import { ITopic } from "aws-cdk-lib/aws-sns";
import { ServiceProps } from "./service-props";
import { WorkerService } from "../../../../shared/lib/shared-constructs/lib/worker-service";
import { IVpc } from "aws-cdk-lib/aws-ec2";
import { CpuArchitecture, ICluster, OperatingSystemFamily, Secret } from "aws-cdk-lib/aws-ecs";
import { IPrivateDnsNamespace } from "aws-cdk-lib/aws-servicediscovery";

export interface BackgroundWorkersProps {
  cluster: ICluster;
  vpc: IVpc;
  serviceDiscoveryNamespace: IPrivateDnsNamespace;
  serviceDiscoveryName: string;
  deployInPrivateSubnet?: boolean;
  sharedProps: SharedProps;
  serviceProps: ServiceProps;
  sharedEventBus: IEventBus;
  stickerClaimedQueue: IQueue;
  stickerClaimedDLQ: IQueue;
  userRegisteredTopic: ITopic;
  useLambda: boolean;
}

export class BackgroundWorkers extends Construct {
  constructor(scope: Construct, id: string, props: BackgroundWorkersProps) {
    super(scope, id);

    if (props.useLambda) {
      const environmentVariables = {
        POWERTOOLS_SERVICE_NAME: props.sharedProps.serviceName,
        POWERTOOLS_LOG_LEVEL:
          props.sharedProps.environment === "prod" ? "WARN" : "INFO",
        ENV: props.sharedProps.environment,
        ConnectionStrings__messaging: "",
        ConnectionStrings__database:
          props.serviceProps.connectionString.stringValue,
        Aws__UserRegisteredTopicArn: props.userRegisteredTopic.topicArn,
        Aws__StickerClaimedQueueUrl: props.stickerClaimedQueue.queueUrl,
        Aws__StickerClaimedDLQUrl: props.stickerClaimedDLQ.queueUrl,
        DRIVING: "ASPNET",
        DRIVEN: "AWS",
        DISABLE_SSL: "true",
      };

      const stickerClaimedWorker = new InstrumentedLambdaFunction(
        this,
        "StickerClaimedWorkerFunction",
        {
          sharedProps: props.sharedProps,
          handler:
            "Stickerlandia.UserManagement.Lambda::Stickerlandia.UserManagement.Lambda.Sqs_StickerClaimed_Generated::StickerClaimed",
          buildDef: "../../src/Stickerlandia.UserManagement.Lambda/",
          functionName: "sticker-claimed-worker",
          environment: environmentVariables,
          memorySize: 1024,
          timeout: Duration.seconds(25),
          logLevel: props.sharedProps.environment === "prod" ? "WARN" : "INFO",
          onFailure: new SqsDestination(props.stickerClaimedDLQ),
        }
      );

      stickerClaimedWorker.function.addEventSource(
        new SqsEventSource(props.stickerClaimedQueue, {
          batchSize: 10,
          reportBatchItemFailures: true,
        })
      );

      const rule = new Rule(this, "StickerClaimedEventRule", {
        eventBus: props.sharedEventBus,
        ruleName: `${props.sharedProps.serviceName}-${props.sharedProps.environment}-sticker-claimed-rule`,
        eventPattern: {
          source: [`${props.sharedProps.environment}.stickers`],
          detailType: ["users.stickerClaimed.v1"],
        },
      });
      rule.addTarget(new SqsQueue(props.stickerClaimedQueue));

      const outboxWorker = new InstrumentedLambdaFunction(
        this,
        "OutboxWorkerFunction",
        {
          sharedProps: props.sharedProps,
          handler:
            "Stickerlandia.UserManagement.Lambda::Stickerlandia.UserManagement.Lambda.OutboxFunctions_Worker_Generated::Worker",
          buildDef: "../../src/Stickerlandia.UserManagement.Lambda/",
          functionName: "outbox-worker",
          environment: environmentVariables,
          memorySize: 1024,
          timeout: Duration.seconds(50),
          logLevel: props.sharedProps.environment === "prod" ? "WARN" : "INFO",
          onFailure: undefined,
        }
      );
      props.userRegisteredTopic.grantPublish(outboxWorker.function);

      const outboxWorkerSchedule = new Rule(this, "OutboxWorkerSchedule", {
        description: "Trigger outbox worker every 1 minute",
        schedule: Schedule.rate(Duration.minutes(1)),
      });

      // Add the Lambda function as a target
      outboxWorkerSchedule.addTarget(
        new LambdaFunction(outboxWorker.function, {
          retryAttempts: 2,
          event: RuleTargetInput.fromObject({
            run: true,
          }),
        })
      );
    } else {
      const workerService = new WorkerService(
        this,
        "UserServiceWorkerService",
        {
          sharedProps: props.sharedProps,
          vpc: props.vpc,
          cluster: props.cluster,
          image: "ghcr.io/datadog/stickerlandia/user-management-worker",
          imageTag: props.sharedProps.version,
          ddApiKey: props.sharedProps.datadog.apiKeyParameter,
          environmentVariables: {
            POWERTOOLS_SERVICE_NAME: props.sharedProps.serviceName,
            POWERTOOLS_LOG_LEVEL:
              props.sharedProps.environment === "prod" ? "WARN" : "INFO",
            ENV: props.sharedProps.environment,
            DRIVING: "ASPNET",
            DRIVEN: "AGNOSTIC",
            DISABLE_SSL: "true",
          },
          secrets: {
            DD_API_KEY: Secret.fromSsmParameter(
              props.sharedProps.datadog.apiKeyParameter
            ),
            ConnectionStrings__database: Secret.fromSsmParameter(
              props.serviceProps.connectionString
            ),
            ConnectionStrings__messaging: Secret.fromSsmParameter(
              props.serviceProps.messagingConnectionString
            ),
            KAFKA_USERNAME: Secret.fromSsmParameter(
              props.serviceProps.kafkaUsername
            ),
            KAFKA_PASSWORD: Secret.fromSsmParameter(
              props.serviceProps.kafkaPassword
            ),
          },
          serviceDiscoveryNamespace: props.serviceDiscoveryNamespace,
          serviceDiscoveryName: props.serviceDiscoveryName,
          deployInPrivateSubnet: props.deployInPrivateSubnet,
          runtimePlatform: {
            cpuArchitecture: CpuArchitecture.ARM64,
            operatingSystemFamily: OperatingSystemFamily.LINUX,
          }
        }
      );
    }
  }
}
