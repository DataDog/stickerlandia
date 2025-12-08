import { IDistribution } from "aws-cdk-lib/aws-cloudfront";
import { Secret } from "aws-cdk-lib/aws-ecs";
import { IStringParameter, StringParameter } from "aws-cdk-lib/aws-ssm";
import { Construct, IDependable } from "constructs";
import { SharedProps } from "../../../../shared/lib/shared-constructs/lib/shared-props";
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
  jaslConfig: IStringParameter;

  constructor(scope: Construct, id: string, props: SharedProps) {
    super(scope, id);
    this.kafkaBootstrapServers = StringParameter.fromStringParameterName(
      this,
      "KafkaBootstrapServersParam",
      `/stickerlandia/${props.environment}/catalogue/kafka-broker`
    );
    this.jaslConfig = StringParameter.fromStringParameterName(
      this,
      "KafkaJaslConfigParam",
      `/stickerlandia/${props.environment}/catalogue/kafka-jasl-config`
    );
  }

  public asSecrets(): { [key: string]: Secret } {
    return {
      QUARKUS_KAFKA_STREAMS_BOOTSTRAP_SERVERS: Secret.fromSsmParameter(
        this.kafkaBootstrapServers
      ),
      KAFKA_BOOTSTRAP_SERVERS: Secret.fromSsmParameter(
        this.kafkaBootstrapServers
      ),
      KAFKA_SASL_JAAS_CONFIG: Secret.fromSsmParameter(this.jaslConfig),
      MP_MESSAGING_CONNECTOR_SMALLRYE_KAFKA_BOOTSTRAP_SERVERS:
        Secret.fromSsmParameter(this.kafkaBootstrapServers),
      MP_MESSAGING_CONNECTOR_SMALLRYE_KAFKA_SASL_JAAS_CONFIG:
        Secret.fromSsmParameter(this.jaslConfig),
    };
  }

  public asEnvironmentVariables(): { [key: string]: string } {
    return {
      KAFKA_SASL_MECHANISM: "PLAIN",
      KAFKA_SECURITY_PROTOCOL: "SASL_SSL",
      MP_MESSAGING_CONNECTOR_SMALLRYE_KAFKA_SECURITY_PROTOCOL: "SASL_SSL",
      MP_MESSAGING_CONNECTOR_SMALLRYE_KAFKA_SASL_MECHANISM: "PLAIN",
    };
  }

  public grantPermissions(grantable: IGrantable): void {
    this.kafkaBootstrapServers.grantRead(grantable);
    this.jaslConfig.grantRead(grantable);
  }
}

export interface ServiceProps {
  cloudfrontDistribution: IDistribution;
  databaseCredentials: DatabaseCredentials;
  messagingProps: MessagingProps;
  serviceDependencies?: IDependable[];
}
