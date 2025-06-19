import * as cdk from "aws-cdk-lib";
import { Construct } from "constructs";
import { SharedProps } from "./constructs/shared-props";
import { DatadogECSFargate, DatadogLambda } from "datadog-cdk-constructs-v2";
import { SharedResources } from "./sharedResources";
import { Api } from "./api";
import { Cluster } from "aws-cdk-lib/aws-ecs";
import { BackgroundWorkers } from "./background-workers";

// import * as sqs from 'aws-cdk-lib/aws-sqs';

export class UserServiceStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    const serviceName = "user-service";
    const environment = process.env.ENV || "dev";
    const version = process.env.VERSION || "latest";
    const connectionString = process.env.CONNECTION_STRING;

    if (!connectionString) {
      throw new Error("CONNECTION_STRING environment variable is required");
    }

    const sharedResources = new SharedResources(this, "SharedResources", {
      networkName: `${serviceName}-${environment}-vpc`,
    });

    const ddSite = process.env.DD_SITE || "datadoghq.com";
    const ddApiKey = process.env.DD_API_KEY || "";

    const cluster = new Cluster(this, "ApiCluster", {
      vpc: sharedResources.vpc,
      clusterName: `${serviceName}-${environment}`,
    });

    const sharedProps: SharedProps = {
      connectionString,
      serviceName: "user-service",
      environment: process.env.ENV || "dev",
      version: process.env.VERSION || "latest",
      team: "users",
      domain: "users",
      datadog: {
        apiKey: ddApiKey,
        site: ddSite,
        lambda: new DatadogLambda(this, "DatadogLambda", {
          apiKey: ddApiKey,
          site: ddSite,
        }),
        ecsFargate: new DatadogECSFargate({
          // One of the following 3 apiKey params are required
          apiKey: ddApiKey,
          cpu: 256,
          memoryLimitMiB: 512,
          isDatadogEssential: true,
          isDatadogDependencyEnabled: true,
          site: ddSite,
          clusterName: cluster.clusterName,
          environmentVariables: {},
          dogstatsd: {
            isEnabled: true,
          },
          apm: {
            isEnabled: true,
            traceInferredProxyServices: true,
          },
          logCollection: {
            isEnabled: true,
            fluentbitConfig: {
              firelensOptions: {
                enableECSLogMetadata: true,
              },
              logDriverConfig: {
                hostEndpoint: `http-intake.logs.${ddSite}`,
                serviceName: serviceName,
              },
            },
          },
          env: environment,
          service: serviceName,
          version: version,
        }),
      },
    };

    const api = new Api(this, "Api", {
      sharedProps: sharedProps,
      vpc: sharedResources.vpc,
      cluster: cluster,
    });

    const backgroundWorkers = new BackgroundWorkers(this, "BackgroundWorkers", {
      sharedProps: sharedProps,
      sharedEventBus: sharedResources.sharedEventBus,
      stickerClaimedQueue: api.stickerClaimedQueue,
      stickerClaimedDLQ: api.stickerClaimedDLQ,
      userRegisteredTopic: api.userRegisteredTopic,
    });
  }
}
