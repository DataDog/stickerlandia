/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import { Construct } from "constructs";
import { SharedProps } from "../../../../shared/lib/shared-constructs/lib/shared-props";
import { InstrumentedLambdaFunction } from "./constructs/instrumented-function";
import { Duration } from "aws-cdk-lib";
import { SqsDlq} from "aws-cdk-lib/aws-lambda-event-sources";
import { IEventBus } from "aws-cdk-lib/aws-events";
import { ServiceProps } from "./service-props";
import { Queue } from "aws-cdk-lib/aws-sqs";
import { DynamoEventSource } from "aws-cdk-lib/aws-lambda-event-sources";
import { FilterCriteria, FilterRule, StartingPosition } from "aws-cdk-lib/aws-lambda";
import { ITable } from "aws-cdk-lib/aws-dynamodb";

export interface BackgroundWorkersProps {
  sharedProps: SharedProps;
  serviceProps: ServiceProps;
  sharedEventBus: IEventBus;
  printerTable: ITable;
  printJobTable: ITable;
}

export class BackgroundWorkers extends Construct {
  constructor(scope: Construct, id: string, props: BackgroundWorkersProps) {
    super(scope, id);

    // --- Outbox Stream Processing Infrastructure ---

    // DLQ for records that fail stream processing after all retries
    const outboxDlq = new Queue(this, "OutboxStreamDLQ", {
      queueName: `outbox-stream-dlq-${props.sharedProps.environment}`,
      retentionPeriod: Duration.days(14),
    });

    // Stream Lambda processes DynamoDB Streams events from both tables,
    // filters for outbox INSERT records, and publishes to EventBridge
    const outboxStreamLambda = new InstrumentedLambdaFunction(
      this,
      "OutboxStreamFunction",
      {
        sharedProps: props.sharedProps,
        handler:
          "Stickerlandia.PrintService.Lambda::Stickerlandia.PrintService.Lambda.OutboxFunctions_HandleStream_Generated::HandleStream",
        buildDef: "../../src/Stickerlandia.PrintService.Lambda/",
        functionName: "outbox-stream",
        environment: {
          DRIVING: "AWS",
          DRIVEN: "AWS",
          Aws__PrinterTableName: props.printerTable.tableName,
          Aws__PrintJobTableName: props.printJobTable.tableName,
          ...props.serviceProps.messagingConfiguration.asEnvironmentVariables(),
        },
        memorySize: 512,
        timeout: Duration.seconds(30),
        onFailure: undefined,
      },
    );

    // Grant permissions: EventBridge PutEvents + DynamoDB stream read
    props.sharedEventBus.grantPutEventsTo(
      outboxStreamLambda.function,
    );
    props.printerTable.grantStreamRead(outboxStreamLambda.function);
    props.printJobTable.grantStreamRead(outboxStreamLambda.function);

    // Event source filter: only process INSERT events where PK starts with "OUTBOX#"
    const outboxStreamFilter = FilterCriteria.filter({
      eventName: FilterRule.isEqual("INSERT"),
      dynamodb: {
        NewImage: {
          PK: {
            S: FilterRule.beginsWith("OUTBOX#"),
          },
        },
      },
    });

    // Event source mapping: Printers table stream → Lambda
    outboxStreamLambda.function.addEventSource(
      new DynamoEventSource(props.printerTable, {
        startingPosition: StartingPosition.TRIM_HORIZON,
        batchSize: 10,
        maxBatchingWindow: Duration.seconds(5),
        maxRecordAge: Duration.hours(1),
        retryAttempts: 3,
        bisectBatchOnError: true,
        reportBatchItemFailures: true,
        onFailure: new SqsDlq(outboxDlq),
        filters: [outboxStreamFilter],
      }),
    );

    // Event source mapping: PrintJobs table stream → Lambda
    outboxStreamLambda.function.addEventSource(
      new DynamoEventSource(props.printJobTable, {
        startingPosition: StartingPosition.TRIM_HORIZON,
        batchSize: 10,
        maxBatchingWindow: Duration.seconds(5),
        maxRecordAge: Duration.hours(1),
        retryAttempts: 3,
        bisectBatchOnError: true,
        reportBatchItemFailures: true,
        onFailure: new SqsDlq(outboxDlq),
        filters: [outboxStreamFilter],
      }),
    );

  }
}
