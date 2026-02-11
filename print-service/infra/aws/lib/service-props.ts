import { IDistribution } from "aws-cdk-lib/aws-cloudfront";
import { IStringParameter, StringParameter } from "aws-cdk-lib/aws-ssm";
import { Construct, IDependable } from "constructs";
import { SharedProps } from "../../../../shared/lib/shared-constructs/lib/shared-props";
import { Secret } from "aws-cdk-lib/aws-ecs";
import { SharedResources } from "../../../../shared/lib/shared-constructs/lib/shared-resources";
import { IGrantable } from "aws-cdk-lib/aws-iam";
import {
  MessagingProps,
  AWSMessagingProps as AWSMessagingPropsBase,
} from "../../../../shared/lib/shared-constructs/lib/messaging";

// Re-export shared messaging types for convenience
export { MessagingProps };

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

/**
 * .NET-specific AWS messaging configuration.
 *
 * Extends the standard AWSMessagingProps to add the .NET configuration
 * convention (double underscore for nested config) alongside the standard
 * EVENT_BUS_NAME.
 */
export class AWSMessagingProps extends AWSMessagingPropsBase {
  public override asEnvironmentVariables(): { [key: string]: string } {
    return {
      ...super.asEnvironmentVariables(),
      // .NET configuration convention for nested settings
      Aws__EventBusName: super.asEnvironmentVariables()["EVENT_BUS_NAME"],
    };
  }
}

export interface ServiceProps {
  cloudfrontDistribution: IDistribution;
  messagingConfiguration: MessagingProps;
  /** Resources that must be created before the ECS service starts */
  serviceDependencies?: IDependable[];
}
