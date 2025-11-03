import "source-map-support/register";
import * as cdk from "aws-cdk-lib";
import { StickerCatalogueServiceStack, } from "../lib/sticker-catalogue-stack";

const app = new cdk.App();

const stickerCatalogueServiceStack = new StickerCatalogueServiceStack(
  app,
  "StickerCatalogueServiceStack",
  {
    stackName: `StickerCatalogue-${process.env.ENV ?? "dev"}`,
    env: {
      account: process.env.CDK_DEFAULT_ACCOUNT,
      region: process.env.CDK_DEFAULT_REGION,
    },
  }
);
