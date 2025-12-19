/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import "source-map-support/register";
import * as cdk from "aws-cdk-lib";
import { execSync } from "child_process";
import { WebFrontendStack } from "../lib/web-frontend-stack";

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

const app = new cdk.App();

const webFrontendStack = new WebFrontendStack(app, "WebFrontendStack", {
  stackName: `WebFrontend-${process.env.ENV ?? "dev"}`,
  env: {
    account: getAwsAccount(),
    region: process.env.CDK_DEFAULT_REGION,
  },
});

cdk.Tags.of(app).add("project", "stickerlandia");
cdk.Tags.of(app).add("service", "web-frontend");
cdk.Tags.of(app).add("team", "advocacy");
cdk.Tags.of(app).add("env", process.env.ENV ?? "dev");
