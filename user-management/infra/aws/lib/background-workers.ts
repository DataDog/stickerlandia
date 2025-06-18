import { Construct } from "constructs";
import { SharedProps } from "./constructs/shared-props";
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

export interface BackgroundWorkersProps {
  sharedProps: SharedProps;
  sharedEventBus: IEventBus;
  stickerClaimedQueue: IQueue;
  stickerClaimedDLQ: IQueue;
  userRegisteredTopic: ITopic;
}

export class BackgroundWorkers extends Construct {
  constructor(scope: Construct, id: string, props: BackgroundWorkersProps) {
    super(scope, id);

    const environmentVariables = {
      POWERTOOLS_SERVICE_NAME: props.sharedProps.serviceName,
      POWERTOOLS_LOG_LEVEL:
        props.sharedProps.environment === "prod" ? "WARN" : "INFO",
      ENV: props.sharedProps.environment,
      ConnectionStrings__messaging: "",
      ConnectionStrings__database: props.sharedProps.connectionString,
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
        buildDef:
          "../../src/Stickerlandia.UserManagement.Lambda/",
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
        buildDef:
          "../../src/Stickerlandia.UserManagement.Lambda/",
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
  }
}
