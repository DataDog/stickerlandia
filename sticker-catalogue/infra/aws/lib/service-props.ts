import { IDistribution } from "aws-cdk-lib/aws-cloudfront";
import { Secret } from "aws-cdk-lib/aws-ecs";
import { IStringParameter, StringParameter } from "aws-cdk-lib/aws-ssm";
import { Construct } from "constructs";
import { IEventBus } from "aws-cdk-lib/aws-events";
import { SharedResources } from "../../../../shared/lib/shared-constructs/lib/shared-resources";
import { SharedProps } from "../../../../shared/lib/shared-constructs/lib/shared-props";
import { IGrantable } from "aws-cdk-lib/aws-iam";

export interface MessagingProps {
  asSecrets(): { [key: string]: Secret };
  asEnvironmentVariables(): { [key: string]: string };
  grantPermissions(grantable: IGrantable): void;
}

export class KafkaMessagingProps extends Construct implements MessagingProps {
  kafkaBootstrapServers: IStringParameter;
  jaslConfig: IStringParameter;

  constructor(scope: Construct, id: string, props: SharedProps) {
    super(scope, id);
    this.kafkaBootstrapServers = StringParameter.fromStringParameterName(
      this,
      "KafkaBootstrapServersParam",
      `/stickerlandia/${props.environment}/sticker-award/kafka-broker`
    );
    this.jaslConfig = StringParameter.fromStringParameterName(
      this,
      "KafkaJaslConfigParam",
      `/stickerlandia/${props.environment}/sticker-award/kafka-jasl-config`
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

export class AWSMessagingProps extends Construct implements MessagingProps {
  sharedEventBus: IEventBus;

  constructor(scope: Construct, id: string, props: SharedResources) {
    super(scope, id);

    this.sharedEventBus = props.sharedEventBus;
  }

  public asSecrets(): { [key: string]: Secret } {
    return {};
  }

  public asEnvironmentVariables(): { [key: string]: string } {
    return {
      EVENT_BUS_NAME: this.sharedEventBus.eventBusName,
    };
  }

  public grantPermissions(grantable: IGrantable): void {
    this.sharedEventBus.grantPutEventsTo(grantable);
  }
}

export interface ServiceProps {
  cloudfrontDistribution: IDistribution;
  jdbcUrl: IStringParameter;
  dbUsername: IStringParameter;
  dbPassword: IStringParameter;
  messagingProps: MessagingProps;
}
