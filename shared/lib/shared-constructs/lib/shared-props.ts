/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

//
// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2024 Datadog, Inc.
//

import { ICluster } from "aws-cdk-lib/aws-ecs";
import { IStringParameter } from "aws-cdk-lib/aws-ssm";
import { Construct } from "constructs/lib/construct";
import { DatadogECSFargate, DatadogLambda } from "datadog-cdk-constructs-v2";

export class SharedProps {
  team: string;
  domain: string;
  serviceName: string;
  environment: string;
  version: string;
  commitSha: string;
  enableDatadog: boolean;
  datadog: {
    lambda: DatadogLambda;
    ecsFargate: DatadogECSFargate;
    apiKey: string;
    apiKeyParameter: IStringParameter;
    site: string;
  };

  constructor(
    scope: Construct,
    domain: string,
    serviceName: string,
    cluster: ICluster,
    ddApiKey: string,
    ddApiKeyParam: IStringParameter,
    ddSite: string | undefined = undefined,
    enableDatadog: boolean = true
  ) {
    const environment = process.env.ENV || "dev";
    const deployMode = process.env.DEPLOY_MODE;
    let version: string;
    if (deployMode === "local") {
      version = "LOCAL";
    } else if (deployMode === "release") {
      version = "latest";
    } else {
      version = process.env.COMMIT_SHA || "latest";
    }
    const commitSha = process.env.COMMIT_SHA_FULL || "";

    this.datadog = {
      apiKey: ddApiKey,
      apiKeyParameter: ddApiKeyParam,
      site: ddSite ?? "datadoghq.com",
      lambda: new DatadogLambda(scope, "DatadogLambda", {
        apiKey: ddApiKey,
        site: ddSite ?? "datadoghq.com",
        extensionLayerVersion: 92,
        nodeLayerVersion: 132,
        dotnetLayerVersion: 23,
        javaLayerVersion: 25,
        enableColdStartTracing: true,
        enableDatadogTracing: true,
        service: serviceName,
        env: environment,
        version: version,
      }),
      ecsFargate: new DatadogECSFargate({
        // One of the following 3 apiKey params are required
        apiKey: ddApiKey,
        cpu: 256,
        memoryLimitMiB: 512,
        isDatadogEssential: true,
        isDatadogDependencyEnabled: true,
        site: ddSite ?? "datadoghq.com",
        clusterName: cluster.clusterName,
        environmentVariables: {
          DD_APM_IGNORE_RESOURCES: "(GET|HEAD) .*/health$",
        },
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
    };

    this.serviceName = serviceName;
    this.environment = environment;
    this.version = version;
    this.commitSha = commitSha;
    this.team = domain;
    this.domain = domain;
    this.enableDatadog = enableDatadog;
  }

  public generateDatadogLambdaConfigurationFor(
    scope: Construct,
    serviceName: string
  ): DatadogLambda {
    return new DatadogLambda(scope, "DatadogLambda", {
      apiKey: this.datadog.apiKey,
      site: this.datadog.site ?? "datadoghq.com",
      extensionLayerVersion: 92,
      nodeLayerVersion: 132,
      dotnetLayerVersion: 23,
      javaLayerVersion: 25,
      enableColdStartTracing: true,
      enableDatadogTracing: true,
      service: serviceName,
      env: this.environment,
      version: this.version,
    });
  }
}
