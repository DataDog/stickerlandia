import "source-map-support/register";
import * as cdk from "aws-cdk-lib";
import { execSync } from "child_process";
import { WebBackendStack } from "../lib/web-backend-stack";

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

const webBackendStack = new WebBackendStack(
  app,
  "StickerlandiaWebBackendStack",
  {
    stackName: `StickerlandiaWebBackend-${process.env.ENV ?? "dev"}`,
    env: {
      account: getAwsAccount(),
      region: process.env.CDK_DEFAULT_REGION,
    },
  }
);

cdk.Tags.of(webBackendStack).add("env", process.env.ENV || "dev");
cdk.Tags.of(webBackendStack).add("project", "stickerlandia");
cdk.Tags.of(webBackendStack).add("service", "web-backend");
cdk.Tags.of(webBackendStack).add("team", "advocacy");
