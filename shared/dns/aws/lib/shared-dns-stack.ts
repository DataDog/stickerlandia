/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import * as cdk from "aws-cdk-lib";
import { Certificate, CertificateValidation } from "aws-cdk-lib/aws-certificatemanager";
import { PublicHostedZone } from "aws-cdk-lib/aws-route53";
import { StringParameter } from "aws-cdk-lib/aws-ssm";
import { Construct } from "constructs";

export class StickerlandiaSharedDnsStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    const hostedZone = new PublicHostedZone(this, "StickerlandiaHostedZone", {
      zoneName: `stickerlandia.dev`,
    });

    const certificate = new Certificate(this, "StickerlandiaCertificate", {
      domainName: "stickerlandia.dev",
      subjectAlternativeNames: ["*.stickerlandia.dev"],
      validation: CertificateValidation.fromDns(hostedZone),
    });

    const hostedZoneIdParameter = new StringParameter(
      this,
      "HostedZoneIdParameter",
      {
        parameterName: `/stickerlandia/shared-dns/hosted-zone-id`,
        stringValue: hostedZone.hostedZoneId,
      },
    );
    const hostedZoneArnParameter = new StringParameter(
      this,
      "HostedZoneArnParameter",
      {
        parameterName: `/stickerlandia/shared-dns/hosted-zone-arn`,
        stringValue: hostedZone.hostedZoneArn,
      },
    );

    const certificateArnParameter = new StringParameter(
      this,
      "CertificateArnParameter",
      {
        parameterName: `/stickerlandia/shared-dns/certificate-arn`,
        stringValue: certificate.certificateArn,
      },
    );
  }
}
