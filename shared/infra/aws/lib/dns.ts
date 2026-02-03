import {
  DnsValidatedCertificate,
  DnsValidatedCertificateProps,
  ICertificate,
} from "aws-cdk-lib/aws-certificatemanager";
import { CnameRecord, PublicHostedZone } from "aws-cdk-lib/aws-route53";
import { Construct } from "constructs";

export interface DnsProps {
  env: string;
  account: string;
}

export class Dns extends Construct {
  certificate?: DnsValidatedCertificate;
  hostedZone: PublicHostedZone;

  constructor(scope: Construct, id: string, props: DnsProps) {
    super(scope, id);

    this.hostedZone = new PublicHostedZone(this, "StickerlandiaHostedZone", {
      zoneName: `stickerlandia.dev`,
    });

    this.certificate = new DnsValidatedCertificate(
      this,
      "MyDnsValidatedCertificate",
      {
        domainName: Dns.getPrimaryDomainName(props.env)!,
        hostedZone: this.hostedZone,
      },
    );
  }

  static getPrimaryDomainName(env: string): string | undefined {
    return env === "prod"
      ? "app.stickerlandia.dev"
      : `${env}.stickerlandia.dev`;
  }
}
