import { Duration } from "aws-cdk-lib";
import { Certificate, ICertificate } from "aws-cdk-lib/aws-certificatemanager";
import { Distribution, IDistribution } from "aws-cdk-lib/aws-cloudfront";
import { CnameRecord, PublicHostedZone } from "aws-cdk-lib/aws-route53";
import { Construct } from "constructs";

export interface DnsProps {
  env: string;
  account: string;
}

export class Dns extends Construct {
  certificate?: ICertificate;
  hostedZone?: PublicHostedZone;

  constructor(scope: Construct, id: string, props: DnsProps) {
    super(scope, id);

    const hostedZoneId = process.env.HOSTED_ZONE_ID!;
    const certificateArn = process.env.CERTIFICATE_ARN!;

    if (hostedZoneId && certificateArn) {
      this.hostedZone = PublicHostedZone.fromHostedZoneAttributes(
        this,
        "ImportedHostedZone",
        {
          hostedZoneId: hostedZoneId,
          zoneName: "stickerlandia.dev",
        },
      ) as PublicHostedZone;

      this.certificate = Certificate.fromCertificateArn(
        this,
        "ImportedCertificate",
        certificateArn,
      );
    } else {
      this.certificate = undefined;
      this.hostedZone = undefined;
    }
  }

  addCnameFor(distribution: Distribution) {
    // Add a CName if the hosted zone exists.
    if (this.hostedZone) {
      const cNameRecord = new CnameRecord(this, "CnameRecord", {
        zone: this.hostedZone!,
        domainName: distribution.domainName,
        recordName: this.getPrimaryDomainName("prod")!,
        ttl: Duration.minutes(5),
      });
    }
  }

  getPrimaryDomainName(env: string): string | undefined {
    return this.certificate
      ? env === "prod"
        ? "app.stickerlandia.dev"
        : `${env}.stickerlandia.dev`
      : undefined;
  }
}
