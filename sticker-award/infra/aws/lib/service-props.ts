import { IDistribution } from "aws-cdk-lib/aws-cloudfront";
import { IStringParameter, StringParameter } from "aws-cdk-lib/aws-ssm";

export interface ServiceProps {
  databaseHost: IStringParameter;
  databaseName: IStringParameter;
  databasePort: string;
  dbUsername: IStringParameter;
  dbPassword: IStringParameter;
  kafkaBootstrapServers: IStringParameter;
  kafkaUsername: IStringParameter;
  kafkaPassword: IStringParameter;
  cloudfrontDistribution: IDistribution;
}