import { IDistribution } from "aws-cdk-lib/aws-cloudfront";
import { IStringParameter } from "aws-cdk-lib/aws-ssm";

export interface ServiceProps {
  cloudfrontDistribution: IDistribution;
  jdbcUrl: IStringParameter;
  dbUsername: IStringParameter;
  dbPassword: IStringParameter;
  kafkaBootstrapServers: IStringParameter;
  kafkaUsername: IStringParameter;
  kafkaPassword: IStringParameter;
  jaslConfig: IStringParameter
}