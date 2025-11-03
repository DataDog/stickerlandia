/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import * as cdk from "aws-cdk-lib";
import { StickerlandiaSharedResourcesStack } from "../lib/shared-resources-stack";
import { AwsSolutionsChecks, ServerlessChecks } from "cdk-nag";

const env = process.env.ENV || "dev";

const app = new cdk.App();
new StickerlandiaSharedResourcesStack(
  app,
  `StickerlandiaSharedResources-${env}`,
  {}
);
// cdk.Aspects.of(app).add(new AwsSolutionsChecks({ verbose: true }));
// cdk.Aspects.of(app).add(new ServerlessChecks({ verbose: true }));

cdk.Tags.of(app).add("project", "stickerlandia");
cdk.Tags.of(app).add("team", "platform");
cdk.Tags.of(app).add("env", env);
