import { IDistribution } from "aws-cdk-lib/aws-cloudfront";
import { IEventBus } from "aws-cdk-lib/aws-events";
import { IStringParameter, StringParameter } from "aws-cdk-lib/aws-ssm";

export interface ServiceProps {
  databaseHost: IStringParameter;
  databaseName: IStringParameter;
  databasePort: string;
  dbUsername: IStringParameter;
  dbPassword: IStringParameter;
  kafkaBootstrapServers: IStringParameter | undefined;
  kafkaUsername: IStringParameter | undefined;
  kafkaPassword: IStringParameter | undefined;
  cloudfrontDistribution: IDistribution;
}