import { IDistribution } from "aws-cdk-lib/aws-cloudfront";
import { IStringParameter, StringParameter } from "aws-cdk-lib/aws-ssm";
import { ISecret } from "aws-cdk-lib/aws-secretsmanager";
import { Construct, IDependable } from "constructs";
import { SharedProps } from "../../../../shared/lib/shared-constructs/lib/shared-props";
import { Secret } from "aws-cdk-lib/aws-ecs";
import { IGrantable } from "aws-cdk-lib/aws-iam";
import { DatabaseCredentials } from "../../../../shared/lib/shared-constructs/lib/database-credentials";
import {
  MessagingProps,
  AWSMessagingProps,
} from "../../../../shared/lib/shared-constructs/lib/messaging";

// Re-export shared messaging types for convenience
export { MessagingProps, AWSMessagingProps };

export class KafkaMessagingProps extends Construct implements MessagingProps {
  kafkaBootstrapServers: IStringParameter;
  kafkaUsername: IStringParameter;
  kafkaPassword: IStringParameter;

  constructor(scope: Construct, id: string, props: SharedProps) {
    super(scope, id);
    this.kafkaBootstrapServers = StringParameter.fromStringParameterName(
      this,
      "KafkaBootstrapServersParam",
      `/stickerlandia/${props.environment}/sticker-award/kafka-broker`
    );
    this.kafkaUsername = StringParameter.fromStringParameterName(
      this,
      "KafkaUsernameParam",
      `/stickerlandia/${props.environment}/sticker-award/kafka-username`
    );
    this.kafkaPassword = StringParameter.fromStringParameterName(
      this,
      "KafkaPasswordParam",
      `/stickerlandia/${props.environment}/sticker-award/kafka-password`
    );
  }

  public asSecrets(): { [key: string]: Secret } {
    return {
      KAFKA_BOOTSTRAP_SERVERS: Secret.fromSsmParameter(
        this.kafkaBootstrapServers
      ),
      KAFKA_USERNAME: Secret.fromSsmParameter(this.kafkaUsername),
      KAFKA_PASSWORD: Secret.fromSsmParameter(this.kafkaPassword),
    };
  }

  public asEnvironmentVariables(): { [key: string]: string } {
    return {
      MESSAGING_PROVIDER: "kafka",
      KAFKA_SECURITY_PROTOCOL: "SASL_SSL",
      KAFKA_GROUP_ID: "sticker-award-service",
      KAFKA_SASL_MECHANISM: "PLAIN",
      KAFKA_ENABLE_TLS: "true",
    };
  }

  public grantPermissions(grantable: IGrantable): void {
    this.kafkaBootstrapServers.grantRead(grantable);
    this.kafkaUsername.grantRead(grantable);
    this.kafkaPassword.grantRead(grantable);
  }
}

export interface ServiceProps {
  connectionStringSecret: ISecret;
  cloudfrontDistribution: IDistribution;
  cloudfrontEndpoint: string;
  messagingConfiguration: MessagingProps;
  /** The database credentials construct - used for granting read permissions to ECS execution role */
  databaseCredentials: DatabaseCredentials;
  /** Resources that must be created before the ECS service starts */
  serviceDependencies?: IDependable[];
}
