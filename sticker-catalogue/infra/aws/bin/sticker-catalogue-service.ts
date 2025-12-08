import "source-map-support/register";
import * as cdk from "aws-cdk-lib";
import { StickerCatalogueServiceStack, } from "../lib/sticker-catalogue-stack";
import { getAwsAccount } from "../../../../shared/lib/shared-constructs/lib/utils";

const app = new cdk.App();

const stickerCatalogueServiceStack = new StickerCatalogueServiceStack(
  app,
  "StickerCatalogueServiceStack",
  {
    stackName: `StickerCatalogue-${process.env.ENV ?? "dev"}`,
    env: {
      account: getAwsAccount(),
      region: process.env.CDK_DEFAULT_REGION,
    },
  }
);

cdk.Tags.of(app).add("project", "stickerlandia");
cdk.Tags.of(app).add("service", "catalogue");
cdk.Tags.of(app).add("team", "advocacy");
cdk.Tags.of(app).add("env", process.env.ENV ?? "dev");
