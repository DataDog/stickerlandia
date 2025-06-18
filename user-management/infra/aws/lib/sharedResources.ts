import { IVpc, SubnetType, Vpc } from "aws-cdk-lib/aws-ec2";
import { EventBus, IEventBus } from "aws-cdk-lib/aws-events";
import { Construct } from "constructs";
export interface SharedResourcesProps {
    networkName: string;
}

// TODO: move the creation of these resources to a seperate stack and update this stack to pull from SSM parameters
export class SharedResources extends Construct {
  vpc: IVpc;
  sharedEventBus: IEventBus; // Placeholder for shared EventBus ARN or name
  constructor(scope: Construct, id: string, props: SharedResourcesProps) {
    super(scope, id);

    this.sharedEventBus = new EventBus(this, "UserManagementEventBus", {
      eventBusName: `stickerlandia-shared-event-bus`,
    });

    // TODO: Add creation of ALB

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
