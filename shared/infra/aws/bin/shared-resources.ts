/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import * as cdk from "aws-cdk-lib";
import { execSync } from "child_process";
import { StickerlandiaSharedResourcesStack } from "../lib/shared-resources-stack";
import { AwsSolutionsChecks, ServerlessChecks } from "cdk-nag";

// Get AWS account from STS if CDK_DEFAULT_ACCOUNT is not set (e.g., with SSO credentials)
function getAwsAccount(): string | undefined {
  if (process.env.CDK_DEFAULT_ACCOUNT) {
    return process.env.CDK_DEFAULT_ACCOUNT;
  }
  try {
    return execSync("aws sts get-caller-identity --query Account --output text", {
      encoding: "utf-8",
      stdio: ["pipe", "pipe", "pipe"],
    }).trim();
  } catch {
    return undefined;
  }
}

const env = process.env.ENV || "dev";

const app = new cdk.App();
new StickerlandiaSharedResourcesStack(
  app,
  `StickerlandiaSharedResources-${env}`,
  {
    env: {
      account: getAwsAccount(),
      region: process.env.CDK_DEFAULT_REGION,
    },
  }
);
// cdk.Aspects.of(app).add(new AwsSolutionsChecks({ verbose: true }));
// cdk.Aspects.of(app).add(new ServerlessChecks({ verbose: true }));

cdk.Tags.of(app).add("project", "stickerlandia");
cdk.Tags.of(app).add("service", "shared-infra");
cdk.Tags.of(app).add("team", "advocacy");
cdk.Tags.of(app).add("env", env);
