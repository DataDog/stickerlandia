import "source-map-support/register";
import * as cdk from "aws-cdk-lib";
import { execSync } from "child_process";
import { UserServiceStack } from "../lib/user-service-stack";

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

const userServiceStack = new UserServiceStack(app, "UserServiceStack", {
  stackName: `UserService-${process.env.ENV ?? "dev"}`,
  env: {
    account: getAwsAccount(),
    region: process.env.CDK_DEFAULT_REGION,
  },
});

cdk.Tags.of(app).add("project", "stickerlandia");
cdk.Tags.of(app).add("service", "user");
cdk.Tags.of(app).add("team", "advocacy");
cdk.Tags.of(app).add("env", process.env.ENV ?? "dev");
