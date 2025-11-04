import "source-map-support/register";
import * as cdk from "aws-cdk-lib";
import { StickerAwardServiceStack } from "../lib/sticker-award-service-stack";

const app = new cdk.App();

const stickerAwardServiceStack = new StickerAwardServiceStack(
  app,
  "StickerAwardServiceStack",
  {
    stackName: `StickerAward-${process.env.ENV ?? "dev"}`,
    env: {
      account: process.env.CDK_DEFAULT_ACCOUNT,
      region: process.env.CDK_DEFAULT_REGION,
    },
  }
);

cdk.Tags.of(app).add("project", "stickerlandia");
cdk.Tags.of(app).add("service", "award");
cdk.Tags.of(app).add("team", "advocacy");
cdk.Tags.of(app).add("env", process.env.ENV ?? "dev");