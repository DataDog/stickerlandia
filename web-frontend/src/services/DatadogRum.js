// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.
import { datadogRum } from '@datadog/browser-rum';
import { reactPlugin } from '@datadog/browser-rum-react';


export const initializeDatadogRum = () => {

  const applicationId = import.meta.env.DD_RUM_APPLICATION_ID;
  const clientToken = import.meta.env.DD_RUM_CLIENT_TOKEN;
  const environment = import.meta.env.DD_ENV || 'development';
  const version = import.meta.env.DD_VERSION || '1.0.0';
  const service = import.meta.env.DD_SERVICE || 'web-frontend';
  const site = import.meta.env.DD_SITE || 'datadoghq.eu';

  if (!applicationId || !clientToken) {
    console.warn('Datadog RUM not initialized: missing application ID or client token');
    return;
  }

  datadogRum.init({
    applicationId,
    clientToken,
    site,
    service,
    env: environment,
    version,
    sessionSampleRate: 100,
    sessionReplaySampleRate: 20,
    trackUserInteractions: true,
    trackResources: true,
    trackLongTasks: true,
    defaultPrivacyLevel: 'mask-user-input',
    allowedTracingUrls: [(url) => true],
    // TODO - turn this on if we start using the react router!
    //plugins: [reactPlugin({ router: true })],
  });

  // Start collecting RUM data
  datadogRum.startSessionReplayRecording();
};
