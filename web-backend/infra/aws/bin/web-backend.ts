import "source-map-support/register";
import * as cdk from "aws-cdk-lib";
import { WebBackendStack } from "../lib/web-backend-stack";

const app = new cdk.App();

const webBackendStack = new WebBackendStack(
  app,
  "StickerlandiaWebBackendStack",
  {
    stackName: `StickerlandiaWebBackend-${process.env.ENV ?? "dev"}`,
    env: {
      account: process.env.CDK_DEFAULT_ACCOUNT,
      region: process.env.CDK_DEFAULT_REGION,
    },
  }
);

cdk.Tags.of(webBackendStack).add("env", process.env.ENV || "dev");
cdk.Tags.of(webBackendStack).add("project", "stickerlandia");
cdk.Tags.of(webBackendStack).add("service", "web-backend");
cdk.Tags.of(webBackendStack).add("team", "advocacy");
cdk.Tags.of(webBackendStack).add("primary-contact", "rachel.white@datadoghq.com");
