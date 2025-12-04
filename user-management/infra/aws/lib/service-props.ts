import { IDistribution } from "aws-cdk-lib/aws-cloudfront";
import { IEventBus } from "aws-cdk-lib/aws-events";
import { IStringParameter, StringParameter } from "aws-cdk-lib/aws-ssm";
import { Construct } from "constructs";
import { SharedProps } from "../../../../shared/lib/shared-constructs/lib/shared-props";
import { Secret } from "aws-cdk-lib/aws-ecs";
import { SharedResources } from "../../../../shared/lib/shared-constructs/lib/shared-resources";
import { IGrantable } from "aws-cdk-lib/aws-iam";

export interface MessagingProps {
  asSecrets(): { [key: string]: Secret };
  asEnvironmentVariables(): { [key: string]: string };
  grantPermissions(grantable: IGrantable): void;
}

export class KafkaMessagingProps extends Construct implements MessagingProps {
  kafkaConnectionString: IStringParameter;
  kafkaUsername: IStringParameter;
  kafkaPassword: IStringParameter;

  constructor(scope: Construct, id: string, props: SharedProps) {
    super(scope, id);
    this.kafkaConnectionString = StringParameter.fromStringParameterName(
      this,
      "MessagingConnectionStringParam",
      `/stickerlandia/${props.environment}/users/kafka-broker`
    );
    this.kafkaUsername = StringParameter.fromStringParameterName(
      this,
      "KafkaUsernameParam",
      `/stickerlandia/${props.environment}/users/kafka-username`
    );
    this.kafkaPassword = StringParameter.fromStringParameterName(
      this,
      "KafkaPasswordParam",
      `/stickerlandia/${props.environment}/users/kafka-password`
    );
  }

  public asSecrets(): { [key: string]: Secret } {
    return {
      ConnectionStrings__messaging: Secret.fromSsmParameter(
        this.kafkaConnectionString
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
    this.kafkaConnectionString.grantRead(grantable);
    this.kafkaUsername.grantRead(grantable);
    this.kafkaPassword.grantRead(grantable);
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
      MESSAGING_PROVIDER: "aws",
      Aws__EventBusName: this.sharedEventBus.eventBusName,
    };
  }

  public grantPermissions(grantable: IGrantable): void {
    this.sharedEventBus.grantPutEventsTo(grantable);
  }
}

export interface ServiceProps {
  cloudfrontDistribution: IDistribution;
  connectionString: IStringParameter;
  messagingConfiguration: MessagingProps;
}
