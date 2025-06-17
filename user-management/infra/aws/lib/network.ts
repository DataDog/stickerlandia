import { SubnetType, Vpc } from "aws-cdk-lib/aws-ec2";
import { Construct } from "constructs";
export interface NetworkProps {
    networkName: string;
}

export class Network extends Construct {
  vpc: Vpc;
  constructor(scope: Construct, id: string, props: NetworkProps) {
    super(scope, id);

    this.vpc = new Vpc(this, "Vpc", {
      vpcName: props.networkName,
      maxAzs: 2,
      natGateways: 1, // Use a single NAT Gateway for cost efficiency
      subnetConfiguration: [
        {
          cidrMask: 24,
          name: "Public",
          subnetType: SubnetType.PUBLIC,
        },
        {
          cidrMask: 24,
          name: "Private",
          subnetType: SubnetType.PRIVATE_WITH_EGRESS,
        },
        {
          cidrMask: 24,
          name: "Isolated",
          subnetType: SubnetType.PRIVATE_ISOLATED,
        },
      ],
    });
  }
}
