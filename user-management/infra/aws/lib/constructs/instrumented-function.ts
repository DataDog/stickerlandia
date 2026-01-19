/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

//
// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2024 Datadog, Inc.
//

import { Construct } from "constructs";
import {
  Architecture,
  IDestination,
  LayerVersion,
  Runtime,
} from "aws-cdk-lib/aws-lambda";
import { DockerImage, Duration, RemovalPolicy, Stack, Tags } from "aws-cdk-lib";
import { Alias } from "aws-cdk-lib/aws-kms";
import { SharedProps } from "../../../../../shared/lib/shared-constructs/lib/shared-props";
import { Effect, PolicyStatement } from "aws-cdk-lib/aws-iam";
import { DotNetFunction } from "@aws-cdk/aws-lambda-dotnet";
import { ISecurityGroup, IVpc, SubnetSelection } from "aws-cdk-lib/aws-ec2";

export class InstrumentedLambdaFunctionProps {
  sharedProps: SharedProps;
  handler: string;
  buildDef: string;
  functionName: string;
  environment: { [key: string]: string };
  timeout?: Duration;
  memorySize?: number;
  logLevel?: string;
  onFailure: IDestination | undefined;
  vpc?: IVpc;
  vpcSubnets?: SubnetSelection;
  securityGroups?: ISecurityGroup[];
}

export class InstrumentedLambdaFunction extends Construct {
  function: DotNetFunction;

  constructor(
    scope: Construct,
    id: string,
    props: InstrumentedLambdaFunctionProps
  ) {
    super(scope, id);

    const region = Stack.of(this).region;

    this.function = new DotNetFunction(this, props.functionName, {
      runtime: Runtime.DOTNET_10,
      functionName: `${props.sharedProps.serviceName}-${props.functionName}-${props.sharedProps.environment}`,
      projectDir: props.buildDef,
      handler: props.handler,
      architecture: Architecture.ARM_64,
      memorySize: props.memorySize ?? 512,
      timeout: props.timeout ?? Duration.seconds(29),
      onFailure: props.onFailure,
      vpc: props.vpc,
      vpcSubnets: props.vpcSubnets,
      securityGroups: props.securityGroups,
      bundling: {
        dockerImage: DockerImage.fromRegistry("public.ecr.aws/sam/build-dotnet10"),
      },
      environment: {
        AWS_LAMBDA_EXEC_WRAPPER: "/opt/datadog_wrapper",
        POWERTOOLS_SERVICE_NAME: props.sharedProps.serviceName,
        POWERTOOLS_LOG_LEVEL:
          props.logLevel ?? props.sharedProps.environment === "prod"
            ? "WARN"
            : "INFO",
        ENV: props.sharedProps.environment,
        DEPLOYED_AT: new Date().toISOString(),
        BUILD_ID: props.sharedProps.version,
        TEAM: props.sharedProps.team,
        DOMAIN: props.sharedProps.domain,
        DD_API_KEY: props.sharedProps.datadog.apiKey,
        DD_SITE: props.sharedProps.datadog.site,
        DD_DATA_STREAMS_ENABLED: "true",
        DD_CAPTURE_LAMBDA_PAYLOAD: "true",
        DD_APM_REPLACE_TAGS: `[
      {
        "name": "function.request.headers.Authorization",
        "pattern": "(?s).*",
        "repl": "****"
      },
	  {
        "name": "function.request.multiValueHeaders.Authorization",
        "pattern": "(?s).*",
        "repl": "****"
      }
]`,
        ...props.environment,
      },
      layers: [
        LayerVersion.fromLayerVersionArn(
          this,
          "DatadogLayer",
          `arn:aws:lambda:${region}:464622532012:layer:Datadog-Extension-ARM:80`
        ),
        LayerVersion.fromLayerVersionArn(
          this,
          "DDTraceLayer",
          `arn:aws:lambda:${region}:464622532012:layer:dd-trace-dotnet-ARM:20`
        ),
      ],
    });

    this.function.addToRolePolicy(
      new PolicyStatement({
        actions: [
          "logs:CreateLogGroup",
          "logs:CreateLogStream",
          "logs:PutLogEvents",
        ],
        resources: ["arn:aws:logs:*:*:*"],
        effect: Effect.DENY,
      })
    );

    const kmsAlias = Alias.fromAliasName(this, "SSMAlias", "aws/ssm");
    kmsAlias.grantDecrypt(this.function);

    Tags.of(this.function).add("service", props.sharedProps.serviceName);
    Tags.of(this.function).add("env", props.sharedProps.environment);
    Tags.of(this.function).add("version", props.sharedProps.version);
  }
}
