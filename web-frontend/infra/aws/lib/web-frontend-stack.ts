/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import * as cdk from "aws-cdk-lib";
import * as s3 from "aws-cdk-lib/aws-s3";
import * as s3deploy from "aws-cdk-lib/aws-s3-deployment";
import * as cloudfront from "aws-cdk-lib/aws-cloudfront";
import { StringParameter } from "aws-cdk-lib/aws-ssm";
import { Construct } from "constructs";
import * as path from "path";

export class WebFrontendStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    const env = process.env.ENV ?? "dev";

    // Look up the bucket name from SSM parameter (deploy-time resolution)
    const bucketName = StringParameter.valueForStringParameter(
      this,
      `/stickerlandia/${env}/shared/web-frontend-bucket`
    );

    // Look up the CloudFront distribution ID from SSM parameter (deploy-time resolution)
    const distributionId = StringParameter.valueForStringParameter(
      this,
      `/stickerlandia/${env}/shared/cloudfront-id`
    );

    // Import the existing bucket by name
    const bucket = s3.Bucket.fromBucketName(this, "WebFrontendBucket", bucketName);

    // Import the existing CloudFront distribution for invalidation
    const distribution = cloudfront.Distribution.fromDistributionAttributes(
      this,
      "WebFrontendDistribution",
      {
        distributionId: distributionId,
        domainName: "", // Not needed for invalidation
      }
    );

    // Deploy the built frontend assets to S3
    new s3deploy.BucketDeployment(this, "DeployWebFrontend", {
      sources: [s3deploy.Source.asset(path.resolve(__dirname, "../../../dist"))],
      destinationBucket: bucket,
      distribution: distribution,
      distributionPaths: ["/*"], // Invalidate all paths in CloudFront cache
      prune: true, // Remove old files from bucket
    });

    // Output the deployment information
    new cdk.CfnOutput(this, "BucketName", {
      value: bucketName,
      description: "S3 bucket where frontend assets are deployed",
    });

    new cdk.CfnOutput(this, "DistributionId", {
      value: distributionId,
      description: "CloudFront distribution ID",
    });
  }
}
