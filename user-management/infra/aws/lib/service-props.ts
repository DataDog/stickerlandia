import { IDistribution } from "aws-cdk-lib/aws-cloudfront";
import { IStringParameter } from "aws-cdk-lib/aws-ssm";

export interface ServiceProps {
  cloudfrontDistribution: IDistribution;
  connectionString: IStringParameter;
  messagingConnectionString: IStringParameter | undefined;
  kafkaUsername: IStringParameter | undefined;
  kafkaPassword: IStringParameter | undefined;
}
