import "source-map-support/register";
import * as cdk from "aws-cdk-lib";
import { UserServiceStack } from "../lib/user-service-stack";
import { getAwsAccount } from "../../../../shared/lib/shared-constructs/lib/utils";

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
